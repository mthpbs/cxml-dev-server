using Cxml.Protocol.Documents.Orders;

namespace CxmlDevServer.Core.Domain;

/// <summary>A purchase order received through an <c>OrderRequest</c>.</summary>
public class PurchaseOrder : IAuditedEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The sending partner (<c>null</c> for unknown partners).</summary>
    public int? PartnerId { get; set; }

    /// <summary>Navigation to <see cref="PartnerId"/>.</summary>
    public TradingPartner? Partner { get; set; }

    /// <summary>The buyer's <c>orderID</c>.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Current version (<c>orderVersion</c>, or counted up when the buyer sends none).</summary>
    public int Version { get; set; } = 1;

    /// <summary>The <c>type</c> of the latest request.</summary>
    public OrderRequestType Type { get; set; }

    /// <summary>Current state.</summary>
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Received;

    /// <summary>The <c>orderDate</c>.</summary>
    public DateTimeOffset? OrderDate { get; set; }

    /// <summary>The <c>deploymentMode</c> of the latest request.</summary>
    public string? DeploymentMode { get; set; }

    /// <summary>Header total.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Currency of the header total.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>The payloadID of the latest request.</summary>
    public string PayloadId { get; set; } = string.Empty;

    /// <summary>The log entry of the latest request.</summary>
    public int? MessageLogId { get; set; }

    /// <summary>Navigation to <see cref="MessageLogId"/>.</summary>
    public CxmlMessageLog? MessageLog { get; set; }

    /// <summary>JSON of the <c>ShipTo</c> address.</summary>
    public string? ShipToJson { get; set; }

    /// <summary>JSON of the <c>BillTo</c> address.</summary>
    public string? BillToJson { get; set; }

    /// <summary>JSON of the payment data (card numbers are masked).</summary>
    public string? PaymentJson { get; set; }

    /// <summary>JSON of the header <c>Contact</c> elements.</summary>
    public string? ContactsJson { get; set; }

    /// <summary>JSON of the header <c>Comments</c>.</summary>
    public string? CommentsJson { get; set; }

    /// <summary>JSON of the header <c>Extrinsic</c> elements.</summary>
    public string? ExtrinsicsJson { get; set; }

    /// <summary>JSON of the header <c>Shipping</c>.</summary>
    public string? ShippingJson { get; set; }

    /// <summary>JSON of the header <c>Tax</c>.</summary>
    public string? TaxJson { get; set; }

    /// <summary>The order lines of the latest version.</summary>
    public List<PurchaseOrderLine> Lines { get; set; } = [];

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
