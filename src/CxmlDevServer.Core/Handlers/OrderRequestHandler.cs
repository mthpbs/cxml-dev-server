using Cxml.Protocol;
using Cxml.Protocol.Common;
using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Orders;
using CxmlDevServer.Core.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Handlers;

/// <summary>
/// Handles <c>OrderRequest</c>: stores new orders, applies later versions (<c>update</c>), cancels (<c>delete</c>)
/// and answers with a plain <c>Status 200</c> (there is no OrderResponse in cXML). A repeated payloadID is
/// acknowledged without creating anything twice.
/// </summary>
public sealed class OrderRequestHandler : ICxmlRequestHandler
{
    /// <summary>Issue code: the same payloadID was already processed.</summary>
    public const string DuplicatePayloadCode = "ORDER-DUPLICATE-PAYLOAD";

    /// <summary>Issue code: a new order uses an orderID that already exists.</summary>
    public const string OrderExistsCode = "ORDER-EXISTS";

    /// <summary>Issue code: an update or delete refers to an unknown order.</summary>
    public const string UnknownOrderCode = "ORDER-UNKNOWN";

    /// <summary>Issue code: an update carries an orderVersion that is not higher than the stored one.</summary>
    public const string VersionNotNewerCode = "ORDER-VERSION-NOT-NEWER";

    private readonly ICxmlDbContext _db;
    private readonly TimeProvider _time;

    /// <summary>Creates the handler.</summary>
    public OrderRequestHandler(ICxmlDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    /// <inheritdoc />
    public string RequestName => OrderRequest.Name;

    /// <inheritdoc />
    public async Task<CxmlHandlerResult> HandleAsync(CxmlInboundContext context)
    {
        var envelope = context.Envelope!;
        var request = (OrderRequest)envelope.Request!.Payload!;
        var header = request.Header;
        var ct = context.CancellationToken;
        var partnerId = context.Partner?.Id;
        var strict = context.EffectiveValidationMode == ValidationMode.Strict;
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(header.OrderId))
        {
            return CxmlHandlerResult.Fail(CxmlStatusCodes.BadRequest, "OrderRequestHeader@orderID is required.");
        }

        var orderId = header.OrderId.Trim();
        var correlationId = CorrelationIds.ForOrder(partnerId, orderId);

        if (header.Type != OrderRequestType.Delete && request.Items.Count == 0)
        {
            return CxmlHandlerResult.Fail(CxmlStatusCodes.BadRequest, "The OrderRequest contains no ItemOut.", correlationId);
        }

        var alreadyProcessed = await _db.MessageLogs.AnyAsync(
            l => l.Direction == MessageDirection.Inbound
                 && l.DocumentType == OrderRequest.Name
                 && l.PayloadId == envelope.PayloadId
                 && l.PartnerId == partnerId
                 && l.CxmlStatusCode >= 200 && l.CxmlStatusCode < 300,
            ct);
        if (alreadyProcessed)
        {
            issues.Add(Warn(DuplicatePayloadCode, $"payloadID '{envelope.PayloadId}' was already processed; the request was acknowledged without changes."));
            return CxmlHandlerResult.Ok(
                detail: "Duplicate payloadID; already processed.", correlationId: correlationId, issues: issues);
        }

        var existing = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.OrderId == orderId, ct);

        switch (header.Type)
        {
            case OrderRequestType.New when existing is not null:
                if (strict)
                {
                    return CxmlHandlerResult.Fail(
                        CxmlStatusCodes.Conflict, $"Order '{orderId}' already exists; send type=\"update\" to change it.", correlationId);
                }

                issues.Add(Warn(OrderExistsCode, $"Order '{orderId}' already exists; it was replaced by this request."));
                break;

            case OrderRequestType.Update when existing is null:
                if (strict)
                {
                    return CxmlHandlerResult.Fail(
                        CxmlStatusCodes.PreconditionFailed, $"Order '{orderId}' is unknown; an update needs an existing order.", correlationId);
                }

                issues.Add(Warn(UnknownOrderCode, $"Order '{orderId}' is unknown; it was created from this update."));
                break;

            case OrderRequestType.Delete when existing is null:
                if (strict)
                {
                    return CxmlHandlerResult.Fail(
                        CxmlStatusCodes.PreconditionFailed, $"Order '{orderId}' is unknown; nothing to delete.", correlationId);
                }

                issues.Add(Warn(UnknownOrderCode, $"Order '{orderId}' is unknown; there was nothing to delete."));
                return CxmlHandlerResult.Ok(correlationId: correlationId, issues: issues);
        }

        if (existing is not null && header.OrderVersion is { } version && version <= existing.Version
            && header.Type != OrderRequestType.New)
        {
            var message = $"orderVersion {version} is not higher than the stored version {existing.Version}.";
            if (strict)
            {
                return CxmlHandlerResult.Fail(CxmlStatusCodes.Conflict, message, correlationId);
            }

            issues.Add(Warn(VersionNotNewerCode, message));
        }

        var order = existing ?? new PurchaseOrder { PartnerId = partnerId, OrderId = orderId };
        order.Version = header.OrderVersion ?? (existing is null ? 1 : existing.Version + 1);
        order.Type = header.Type;
        order.PayloadId = envelope.PayloadId;
        order.DeploymentMode = envelope.Request.DeploymentMode;
        order.MessageLog = context.LogEntry;

        if (header.Type == OrderRequestType.Delete)
        {
            order.Status = PurchaseOrderStatus.Cancelled;
        }
        else
        {
            ApplyHeader(order, header);
            ReplaceLines(order, request.Items);
            order.Status = header.Type == OrderRequestType.New && existing is null
                ? PurchaseOrderStatus.Received
                : PurchaseOrderStatus.Updated;
        }

        if (existing is null)
        {
            _db.PurchaseOrders.Add(order);
        }

        await _db.SaveChangesAsync(ct);
        return CxmlHandlerResult.Ok(correlationId: correlationId, issues: issues);
    }

    private static ValidationIssue Warn(string code, string message) =>
        new(ValidationSeverity.Warning, code, message, Source: ValidationSource.Rule);

    private static void ApplyHeader(PurchaseOrder order, OrderRequestHeader header)
    {
        order.OrderDate = header.OrderDate;
        order.TotalAmount = header.Total.Amount;
        order.Currency = header.Total.Currency;
        order.ShipToJson = Snapshots.ToJson(header.ShipTo is { } shipTo ? Snapshots.From(shipTo.Address) : null);
        order.BillToJson = Snapshots.ToJson(header.BillTo is { } billTo ? Snapshots.From(billTo.Address) : null);
        order.PaymentJson = Snapshots.ToJson(header.Payment is { } payment ? Snapshots.From(payment) : null);
        order.ContactsJson = Snapshots.ListToJson(header.Contacts.Select(c => Snapshots.From(c)).ToList());
        order.CommentsJson = Snapshots.ListToJson(header.Comments.Select(c => Snapshots.From(c)).ToList());
        order.ExtrinsicsJson = Snapshots.ListToJson(Snapshots.From(header.Extrinsics));
        order.ShippingJson = Snapshots.ToJson(header.Shipping is { } shipping ? Snapshots.From(shipping) : null);
        order.TaxJson = Snapshots.ToJson(header.Tax is { } tax ? Snapshots.From(tax) : null);
    }

    private void ReplaceLines(PurchaseOrder order, IReadOnlyList<ItemOut> items)
    {
        _db.PurchaseOrderLines.RemoveRange(order.Lines);
        order.Lines.Clear();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var detail = item.ItemDetail;
            var description = detail?.Descriptions.FirstOrDefault();
            var classification = detail?.Classifications.FirstOrDefault();
            order.Lines.Add(new PurchaseOrderLine
            {
                LineNumber = item.LineNumber ?? index + 1,
                SupplierPartId = item.ItemId.SupplierPartId,
                SupplierPartAuxiliaryId = item.ItemId.SupplierPartAuxiliaryId,
                BuyerPartId = item.ItemId.BuyerPartId,
                Description = description?.Text is { Length: > 0 } text ? text : description?.ShortName ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = detail?.UnitPrice.Amount ?? 0m,
                Currency = detail?.UnitPrice.Currency ?? order.Currency,
                UnitOfMeasure = detail?.UnitOfMeasure ?? string.Empty,
                ClassificationDomain = classification?.Domain,
                Classification = classification?.Value,
                ManufacturerPartId = detail?.ManufacturerPartId,
                ManufacturerName = detail?.ManufacturerName,
                RequestedDeliveryDate = item.RequestedDeliveryDate,
            });
        }
    }
}
