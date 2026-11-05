using Cxml.Protocol;
using Cxml.Protocol.Common;
using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Writing;
using CxmlDevServer.Core.Common;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Orders;
using Microsoft.Extensions.Options;
using CxmlOptions = CxmlDevServer.Core.Options.CxmlServerOptions;

namespace CxmlDevServer.Core.PunchOut;

/// <summary>The document built for a session, as an envelope and as XML.</summary>
/// <param name="Envelope">The envelope.</param>
/// <param name="Xml">The serialized document.</param>
public sealed record PunchOutOrderMessageDocument(CxmlEnvelope Envelope, string Xml);

/// <summary>Builds the <c>PunchOutOrderMessage</c> that returns a cart to the buyer.</summary>
public sealed class PunchOutOrderMessageBuilder
{
    private readonly SupplierIdentityService _supplierIdentities;
    private readonly CxmlWriter _writer;
    private readonly CxmlOptions _options;
    private readonly TimeProvider _time;

    /// <summary>Creates the builder.</summary>
    public PunchOutOrderMessageBuilder(
        SupplierIdentityService supplierIdentities,
        CxmlWriter writer,
        IOptions<CxmlOptions> options,
        TimeProvider time)
    {
        _supplierIdentities = supplierIdentities;
        _writer = writer;
        _options = options.Value;
        _time = time;
    }

    /// <summary>
    /// Builds the message for <paramref name="session"/> (whose <c>Lines</c> and <c>Partner</c> must be loaded).
    /// For a cancelled session the message carries an empty cart (create) or the original items with
    /// <c>Status 204</c> (edit and inspect).
    /// </summary>
    public async Task<PunchOutOrderMessageDocument> BuildAsync(
        PunchOutSession session, bool cancelled, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierIdentities.GetDefaultAsync(cancellationToken);
        var partner = session.Partner;

        var buyerDomain = session.BuyerDomain ?? partner?.FromDomain;
        var buyerIdentity = session.BuyerIdentity ?? partner?.FromIdentity;
        var header = new CxmlHeader
        {
            From = new Party { Credentials = [new Credential { Domain = supplier.Domain, Identity = supplier.Identity }] },
            To = new Party
            {
                Credentials = string.IsNullOrEmpty(buyerDomain) || string.IsNullOrEmpty(buyerIdentity)
                    ? []
                    : [new Credential { Domain = buyerDomain, Identity = buyerIdentity }],
            },
            Sender = new SenderParty
            {
                Credentials = [new Credential { Domain = supplier.Domain, Identity = supplier.Identity }],
                UserAgent = _options.UserAgent,
            },
        };

        var cart = ResolveCart(session, cancelled);
        var currency = cart.FirstOrDefault()?.Currency is { Length: > 0 } c ? c : _options.DefaultCurrency;
        var message = new PunchOutOrderMessage
        {
            BuyerCookie = session.BuyerCookie,
            Header = new PunchOutOrderMessageHeader
            {
                OperationAllowed = partner?.OperationAllowed is { } allowed and not PunchOutOperation.Unknown
                    ? allowed
                    : PunchOutOperation.Create,
                Total = new Money(cart.Sum(l => l.Quantity * l.UnitPrice), currency),
            },
            Items = cart.Select((line, index) => ToItemIn(line, index + 1)).ToList(),
        };

        var envelope = new CxmlEnvelope
        {
            PayloadId = PayloadIdGenerator.Generate(),
            Timestamp = _time.GetLocalNow(),
            Lang = "en-US",
            Header = header,
            Message = new CxmlMessage
            {
                DeploymentMode = session.DeploymentMode,
                InReplyTo = session.SetupPayloadId,
                Status = cancelled && session.Operation != PunchOutOperation.Create
                    ? new Status { Code = CxmlStatusCodes.NoContent, Text = CxmlStatusCodes.DefaultText(CxmlStatusCodes.NoContent), Lang = "en-US" }
                    : null,
                Payload = message,
            },
        };
        return new PunchOutOrderMessageDocument(envelope, _writer.Write(envelope));
    }

    private static List<CartLineSnapshot> ResolveCart(PunchOutSession session, bool cancelled)
    {
        if (!cancelled)
        {
            return session.Lines.OrderBy(l => l.LineNumber).Select(ToSnapshot).ToList();
        }

        return session.Operation == PunchOutOperation.Create
            ? []
            : Snapshots.ListFromJson<CartLineSnapshot>(session.OriginalLinesJson);
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

    private static ItemIn ToItemIn(CartLineSnapshot line, int lineNumber) => new()
    {
        Quantity = line.Quantity,
        LineNumber = lineNumber,
        ItemId = new ItemId
        {
            SupplierPartId = line.SupplierPartId,
            SupplierPartAuxiliaryId = string.IsNullOrWhiteSpace(line.SupplierPartAuxiliaryId) ? null : line.SupplierPartAuxiliaryId,
        },
        ItemDetail = new ItemDetail
        {
            UnitPrice = new Money(line.UnitPrice, line.Currency),
            Descriptions =
            [
                new Description("en", string.IsNullOrWhiteSpace(line.Description) ? line.ShortName : line.Description, NullIfEmpty(line.ShortName)),
            ],
            UnitOfMeasure = line.UnitOfMeasure,
            Classifications = [new Classification(line.ClassificationDomain ?? "UNSPSC", line.Unspsc ?? string.Empty)],
            ManufacturerPartId = NullIfEmpty(line.ManufacturerPartId),
            ManufacturerName = NullIfEmpty(line.ManufacturerName),
            LeadTime = line.LeadTimeDays,
        },
    };

    private static string? NullIfEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;
}
