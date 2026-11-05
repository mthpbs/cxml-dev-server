using Cxml.Protocol.Documents.PunchOut;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.PunchOut;

/// <summary>
/// The PunchOut session logic behind the shop UI: session lookup with expiry, cart changes, and returning the cart
/// (checkout) or leaving without ordering (cancel) as a PunchOutOrderMessage.
/// </summary>
public sealed class PunchOutService
{
    private readonly ICxmlDbContext _db;
    private readonly PunchOutOrderMessageBuilder _builder;
    private readonly TimeProvider _time;

    /// <summary>Creates the service.</summary>
    public PunchOutService(ICxmlDbContext db, PunchOutOrderMessageBuilder builder, TimeProvider time)
    {
        _db = db;
        _builder = builder;
        _time = time;
    }

    /// <summary>
    /// Finds a session by token (with cart and partner) in any state; a session past its expiry is moved to
    /// <see cref="PunchOutSessionStatus.Expired"/> first. <c>null</c> when the token is unknown.
    /// </summary>
    public async Task<PunchOutSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var session = await _db.PunchOutSessions
            .Include(s => s.Lines.OrderBy(l => l.LineNumber))
            .Include(s => s.Partner)
            .FirstOrDefaultAsync(s => s.Token == token, cancellationToken);
        if (session is { IsOpenForShopping: true } && _time.GetUtcNow() >= session.ExpiresAt)
        {
            session.Status = PunchOutSessionStatus.Expired;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return session;
    }

    /// <summary>The session when it can still be used for shopping (not expired, submitted or cancelled); otherwise <c>null</c>.</summary>
    public async Task<PunchOutSession?> GetActiveSessionAsync(string token, CancellationToken cancellationToken = default) =>
        await GetSessionAsync(token, cancellationToken) is { IsOpenForShopping: true } session ? session : null;

    /// <summary>
    /// Opens the session for shopping: the first call marks it <see cref="PunchOutSessionStatus.Opened"/>; reloads are
    /// allowed until the session is submitted, cancelled or expired.
    /// </summary>
    /// <exception cref="PunchOutSessionException">The session is unknown, expired or finished.</exception>
    public async Task<PunchOutSession> OpenAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = await RequireActiveAsync(token, cancellationToken);
        if (session.Status == PunchOutSessionStatus.Created)
        {
            session.Status = PunchOutSessionStatus.Opened;
            session.OpenedAt = _time.GetUtcNow();
            await _db.SaveChangesAsync(cancellationToken);
        }

        return session;
    }

    /// <summary>Adds a catalog item to the cart; an item already in the cart has its quantity increased.</summary>
    /// <exception cref="PunchOutSessionException">The session cannot be changed or the item does not exist.</exception>
    public async Task<PunchOutCartLine> AddToCartAsync(
        string token, int catalogItemId, decimal quantity = 1, CancellationToken cancellationToken = default)
    {
        var session = await RequireEditableAsync(token, cancellationToken);
        RequirePositive(quantity);
        var item = await _db.CatalogItems.FirstOrDefaultAsync(i => i.Id == catalogItemId && i.IsActive, cancellationToken)
                   ?? throw new PunchOutSessionException(PunchOutError.ItemNotFound, $"Catalog item {catalogItemId} does not exist.");

        var line = session.Lines.FirstOrDefault(l => l.CatalogItemId == item.Id);
        if (line is null)
        {
            line = new PunchOutCartLine
            {
                LineNumber = session.Lines.Count == 0 ? 1 : session.Lines.Max(l => l.LineNumber) + 1,
                CatalogItemId = item.Id,
                SupplierPartId = item.SupplierPartId,
                SupplierPartAuxiliaryId = string.IsNullOrWhiteSpace(item.SupplierPartAuxiliaryId)
                    ? PunchOutCookies.ForCatalogItem(item.Id)
                    : item.SupplierPartAuxiliaryId,
                ShortName = item.ShortName,
                Description = item.Description,
                Quantity = quantity,
                UnitPrice = item.UnitPrice,
                Currency = item.Currency,
                UnitOfMeasure = item.UnitOfMeasure,
                Unspsc = item.Unspsc,
                ManufacturerName = item.ManufacturerName,
                ManufacturerPartId = item.ManufacturerPartId,
                LeadTimeDays = item.LeadTimeDays,
            };
            session.Lines.Add(line);
        }
        else
        {
            line.Quantity += quantity;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return line;
    }

    /// <summary>Sets the quantity of a cart line; a quantity of zero or less removes the line.</summary>
    /// <exception cref="PunchOutSessionException">The session cannot be changed or the line does not exist.</exception>
    public async Task SetQuantityAsync(string token, int lineId, decimal quantity, CancellationToken cancellationToken = default)
    {
        var session = await RequireEditableAsync(token, cancellationToken);
        var line = FindLine(session, lineId);
        if (quantity <= 0)
        {
            session.Lines.Remove(line);
            _db.PunchOutCartLines.Remove(line);
        }
        else
        {
            line.Quantity = quantity;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Removes a cart line.</summary>
    /// <exception cref="PunchOutSessionException">The session cannot be changed or the line does not exist.</exception>
    public async Task RemoveLineAsync(string token, int lineId, CancellationToken cancellationToken = default)
    {
        var session = await RequireEditableAsync(token, cancellationToken);
        var line = FindLine(session, lineId);
        session.Lines.Remove(line);
        _db.PunchOutCartLines.Remove(line);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Empties the cart.</summary>
    /// <exception cref="PunchOutSessionException">The session cannot be changed.</exception>
    public async Task ClearCartAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = await RequireEditableAsync(token, cancellationToken);
        _db.PunchOutCartLines.RemoveRange(session.Lines);
        session.Lines.Clear();
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Builds the PunchOutOrderMessage for the current cart without sending or logging it (preview).</summary>
    public Task<PunchOutOrderMessageDocument> BuildOrderMessageAsync(PunchOutSession session, CancellationToken cancellationToken = default) =>
        _builder.BuildAsync(session, cancelled: false, cancellationToken);

    /// <summary>
    /// Returns the cart to the buyer: marks the session submitted, logs the outbound message and returns what is
    /// needed to render the auto-submitting form.
    /// </summary>
    /// <exception cref="PunchOutSessionException">The session is read-only (inspect), unknown, expired or finished.</exception>
    public async Task<PunchOutReturn> CheckoutAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = await RequireEditableAsync(token, cancellationToken);
        var url = session.BrowserFormPostUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new PunchOutSessionException(PunchOutError.Invalid, "The session has no BrowserFormPost URL to return the cart to.");
        }

        var document = await _builder.BuildAsync(session, cancelled: false, cancellationToken);
        session.Status = PunchOutSessionStatus.Submitted;
        return await CompleteAsync(session, document, url, cancelled: false, cancellationToken);
    }

    /// <summary>
    /// Leaves the session without ordering. For create the buyer receives an empty cart, for edit the original items
    /// with Status 204. Inspect sessions have no return URL: they are just closed and <c>null</c> is returned.
    /// </summary>
    /// <exception cref="PunchOutSessionException">The session is unknown, expired or finished.</exception>
    public async Task<PunchOutReturn?> CancelAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = await RequireActiveAsync(token, cancellationToken);
        session.Status = PunchOutSessionStatus.Cancelled;
        if (string.IsNullOrWhiteSpace(session.BrowserFormPostUrl))
        {
            session.CompletedAt = _time.GetUtcNow();
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        var document = await _builder.BuildAsync(session, cancelled: true, cancellationToken);
        return await CompleteAsync(session, document, session.BrowserFormPostUrl, cancelled: true, cancellationToken);
    }

    private async Task<PunchOutReturn> CompleteAsync(
        PunchOutSession session, PunchOutOrderMessageDocument document, string url, bool cancelled, CancellationToken cancellationToken)
    {
        var encoding = session.Partner?.PunchOutPostEncoding == PunchOutPostEncoding.UrlEncoded
            ? PunchOutFormEncoding.UrlEncoded
            : PunchOutFormEncoding.Base64;
        var field = PunchOutFormEncoder.Encode(document.Xml, encoding);
        var now = _time.GetUtcNow();
        session.CompletedAt = now;

        var log = new CxmlMessageLog
        {
            Direction = MessageDirection.Outbound,
            Channel = MessageChannel.BrowserPost,
            DocumentType = PunchOutOrderMessage.Name,
            PayloadId = document.Envelope.PayloadId,
            InReplyTo = session.SetupPayloadId,
            CorrelationId = session.CorrelationId,
            PartnerId = session.PartnerId,
            RemoteEndpoint = url,
            CxmlStatusCode = document.Envelope.Message?.Status?.Code,
            CxmlStatusText = document.Envelope.Message?.Status?.Text,
            RawXml = document.Xml,
            ContentType = "application/x-www-form-urlencoded",
            Notes = (cancelled ? "Cart not ordered (cancelled)" : "Cart returned") + $"; encoded as {field.Key}.",
            CreatedAt = now,
        };
        _db.MessageLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        return new PunchOutReturn(url, field.Key, field.Value, encoding, document.Xml, log.Id, cancelled);
    }

    private async Task<PunchOutSession> RequireActiveAsync(string token, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(token, cancellationToken)
                      ?? throw new PunchOutSessionException(PunchOutError.NotFound, "This PunchOut session does not exist.");
        return session.Status switch
        {
            PunchOutSessionStatus.Expired => throw new PunchOutSessionException(PunchOutError.Expired, "This PunchOut session has expired."),
            PunchOutSessionStatus.Submitted or PunchOutSessionStatus.Cancelled =>
                throw new PunchOutSessionException(PunchOutError.Completed, "This PunchOut session has already been completed."),
            _ => session,
        };
    }

    private async Task<PunchOutSession> RequireEditableAsync(string token, CancellationToken cancellationToken)
    {
        var session = await RequireActiveAsync(token, cancellationToken);
        return session.IsReadOnly
            ? throw new PunchOutSessionException(PunchOutError.ReadOnly, "This is an inspect session; the cart is read-only.")
            : session;
    }

    private static PunchOutCartLine FindLine(PunchOutSession session, int lineId) =>
        session.Lines.FirstOrDefault(l => l.Id == lineId)
        ?? throw new PunchOutSessionException(PunchOutError.ItemNotFound, $"Cart line {lineId} does not exist.");

    private static void RequirePositive(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new PunchOutSessionException(PunchOutError.Invalid, "The quantity must be greater than zero.");
        }
    }
}
