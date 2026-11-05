using System.Buffers.Text;
using System.Security.Cryptography;
using Cxml.Protocol;
using Cxml.Protocol.Common;
using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Orders;
using CxmlDevServer.Core.Pipeline;
using CxmlDevServer.Core.PunchOut;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Handlers;

/// <summary>
/// Handles <c>PunchOutSetupRequest</c>: creates a shopping session and answers with the StartPage URL. For edit and
/// inspect the cart is rebuilt from the <c>ItemOut</c> elements (by auxiliary cookie, then part number, else ad hoc).
/// </summary>
public sealed class PunchOutSetupHandler : ICxmlRequestHandler
{
    /// <summary>Issue code: edit or inspect without any ItemOut.</summary>
    public const string NoItemsCode = "PUNCHOUT-NO-ITEMS";

    /// <summary>Issue code: an ItemOut could not be matched and has no ItemDetail.</summary>
    public const string UnknownItemCode = "PUNCHOUT-UNKNOWN-ITEM";

    private readonly ICxmlDbContext _db;
    private readonly TimeProvider _time;

    /// <summary>Creates the handler.</summary>
    public PunchOutSetupHandler(ICxmlDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    /// <inheritdoc />
    public string RequestName => PunchOutSetupRequest.Name;

    /// <inheritdoc />
    public async Task<CxmlHandlerResult> HandleAsync(CxmlInboundContext context)
    {
        var envelope = context.Envelope!;
        var request = (PunchOutSetupRequest)envelope.Request!.Payload!;
        var ct = context.CancellationToken;
        var issues = new List<ValidationIssue>();

        switch (request.Operation)
        {
            case PunchOutOperation.Source:
                return CxmlHandlerResult.Fail(
                    CxmlStatusCodes.NotImplemented, "The PunchOut operation 'source' is not supported by this server.");
            case PunchOutOperation.Unknown:
                return CxmlHandlerResult.Fail(
                    CxmlStatusCodes.BadRequest, "PunchOutSetupRequest@operation must be 'create', 'edit' or 'inspect'.");
        }

        if (string.IsNullOrWhiteSpace(request.BuyerCookie))
        {
            return CxmlHandlerResult.Fail(CxmlStatusCodes.BadRequest, "BuyerCookie is required and must not be empty.");
        }

        if (request.Operation != PunchOutOperation.Inspect)
        {
            var operation = request.Operation.ToCxml();
            if (string.IsNullOrWhiteSpace(request.BrowserFormPostUrl))
            {
                return CxmlHandlerResult.Fail(
                    CxmlStatusCodes.BadRequest,
                    $"BrowserFormPost/URL is required for PunchOut operation '{operation}' so the cart can be returned to the buyer.");
            }

            if (!Uri.TryCreate(request.BrowserFormPostUrl.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            {
                return CxmlHandlerResult.Fail(
                    CxmlStatusCodes.BadRequest, $"BrowserFormPost/URL '{request.BrowserFormPostUrl}' is not an absolute http(s) URL.");
            }
        }

        var lines = new List<PunchOutCartLine>();
        if (request.Operation is PunchOutOperation.Edit or PunchOutOperation.Inspect)
        {
            lines = await BuildLinesAsync(request.Items, issues, ct);
            if (lines.Count == 0)
            {
                const string message = "Operation requires at least one ItemOut with the items to edit or inspect.";
                if (context.EffectiveValidationMode == ValidationMode.Strict)
                {
                    return CxmlHandlerResult.Fail(CxmlStatusCodes.PreconditionFailed, message, issues: issues);
                }

                issues.Add(new ValidationIssue(ValidationSeverity.Warning, NoItemsCode, message, Source: ValidationSource.Rule));
            }
        }

        var now = _time.GetUtcNow();
        var buyer = envelope.Header?.From.Primary;
        var session = new PunchOutSession
        {
            Token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)),
            PartnerId = context.Partner?.Id,
            BuyerCookie = request.BuyerCookie,
            Operation = request.Operation,
            BrowserFormPostUrl = request.BrowserFormPostUrl?.Trim(),
            SetupPayloadId = envelope.PayloadId,
            DeploymentMode = envelope.Request.DeploymentMode,
            BuyerDomain = buyer?.Domain,
            BuyerIdentity = buyer?.Identity,
            ContactsJson = Snapshots.ListToJson(request.Contacts.Select(c => Snapshots.From(c)).ToList()),
            ExtrinsicsJson = Snapshots.ListToJson(Snapshots.From(request.Extrinsics)),
            ShipToJson = Snapshots.ToJson(request.ShipTo is { } shipTo ? Snapshots.From(shipTo.Address) : null),
            SelectedSupplierPartId = request.SelectedItem?.SupplierPartId,
            OriginalLinesJson = request.Operation == PunchOutOperation.Create ? null : Snapshots.ListToJson(lines.Select(ToSnapshot).ToList()),
            Status = PunchOutSessionStatus.Created,
            CorrelationId = Guid.NewGuid().ToString("N"),
            ExpiresAt = now.AddMinutes(Math.Max(1, context.Options.PunchOutSessionMinutes)),
            SetupMessageLog = context.LogEntry,
            Lines = lines,
        };

        _db.PunchOutSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        var startPage = new PunchOutSetupResponse { StartPageUrl = $"{context.BaseUrl}/punchout/{session.Token}" };
        return CxmlHandlerResult.Ok(startPage, correlationId: session.CorrelationId, issues: issues);
    }

    private async Task<List<PunchOutCartLine>> BuildLinesAsync(
        IReadOnlyList<ItemOut> items, List<ValidationIssue> issues, CancellationToken cancellationToken)
    {
        var cookieIds = items
            .Select(i => PunchOutCookies.TryGetCatalogItemId(i.ItemId.SupplierPartAuxiliaryId, out var id) ? id : (int?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToList();
        var partIds = items.Select(i => i.ItemId.SupplierPartId).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        var catalog = await _db.CatalogItems.AsNoTracking()
            .Where(c => cookieIds.Contains(c.Id) || partIds.Contains(c.SupplierPartId))
            .ToListAsync(cancellationToken);

        var lines = new List<PunchOutCartLine>();
        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            var found = PunchOutCookies.TryGetCatalogItemId(item.ItemId.SupplierPartAuxiliaryId, out var catalogId)
                ? catalog.FirstOrDefault(c => c.Id == catalogId)
                : null;
            found ??= catalog.FirstOrDefault(c => string.Equals(c.SupplierPartId, item.ItemId.SupplierPartId, StringComparison.Ordinal));

            var line = found is not null ? FromCatalog(found) : FromItemOut(item, issues);
            line.LineNumber = index + 1;
            line.Quantity = item.Quantity;
            lines.Add(line);
        }

        return lines;
    }

    private static PunchOutCartLine FromCatalog(CatalogItem item) => new()
    {
        CatalogItemId = item.Id,
        SupplierPartId = item.SupplierPartId,
        SupplierPartAuxiliaryId = string.IsNullOrWhiteSpace(item.SupplierPartAuxiliaryId)
            ? PunchOutCookies.ForCatalogItem(item.Id)
            : item.SupplierPartAuxiliaryId,
        ShortName = item.ShortName,
        Description = item.Description,
        UnitPrice = item.UnitPrice,
        Currency = item.Currency,
        UnitOfMeasure = item.UnitOfMeasure,
        Unspsc = item.Unspsc,
        ManufacturerName = item.ManufacturerName,
        ManufacturerPartId = item.ManufacturerPartId,
        LeadTimeDays = item.LeadTimeDays,
    };

    private static PunchOutCartLine FromItemOut(ItemOut item, List<ValidationIssue> issues)
    {
        var detail = item.ItemDetail;
        if (detail is null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning, UnknownItemCode,
                $"Item '{item.ItemId.SupplierPartId}' is not in the catalog and has no ItemDetail; it is kept as an ad-hoc line without price.",
                Source: ValidationSource.Rule));
        }

        var description = detail?.Descriptions.FirstOrDefault();
        var classification = detail?.Classifications.FirstOrDefault();
        return new PunchOutCartLine
        {
            SupplierPartId = item.ItemId.SupplierPartId,
            SupplierPartAuxiliaryId = item.ItemId.SupplierPartAuxiliaryId,
            ShortName = description?.ShortName ?? description?.Text ?? item.ItemId.SupplierPartId,
            Description = description?.Text ?? string.Empty,
            UnitPrice = detail?.UnitPrice.Amount ?? 0m,
            Currency = detail?.UnitPrice.Currency is { Length: > 0 } c ? c : "USD",
            UnitOfMeasure = detail?.UnitOfMeasure is { Length: > 0 } u ? u : "EA",
            Unspsc = classification?.Value,
            ClassificationDomain = classification?.Domain,
            ManufacturerName = detail?.ManufacturerName,
            ManufacturerPartId = detail?.ManufacturerPartId,
            LeadTimeDays = detail?.LeadTime,
            IsAdHoc = true,
        };
    }

    private static CartLineSnapshot ToSnapshot(PunchOutCartLine line) => new()
    {
        CatalogItemId = line.CatalogItemId,
        SupplierPartId = line.SupplierPartId,
        SupplierPartAuxiliaryId = line.SupplierPartAuxiliaryId,
        ShortName = line.ShortName,
        Description = line.Description,
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice,
        Currency = line.Currency,
        UnitOfMeasure = line.UnitOfMeasure,
        Unspsc = line.Unspsc,
        ClassificationDomain = line.ClassificationDomain,
        ManufacturerName = line.ManufacturerName,
        ManufacturerPartId = line.ManufacturerPartId,
        LeadTimeDays = line.LeadTimeDays,
        IsAdHoc = line.IsAdHoc,
    };
}
