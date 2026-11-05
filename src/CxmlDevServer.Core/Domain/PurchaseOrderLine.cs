namespace CxmlDevServer.Core.Domain;

/// <summary>One <c>ItemOut</c> of a purchase order.</summary>
public class PurchaseOrderLine
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Owning order.</summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>Navigation to <see cref="PurchaseOrderId"/>.</summary>
    public PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>The <c>lineNumber</c> (or the position when missing).</summary>
    public int LineNumber { get; set; }

    /// <summary>Supplier part number.</summary>
    public string SupplierPartId { get; set; } = string.Empty;

    /// <summary>Supplier part auxiliary id (inner XML).</summary>
    public string? SupplierPartAuxiliaryId { get; set; }

    /// <summary>Buyer part number.</summary>
    public string? BuyerPartId { get; set; }

    /// <summary>Item description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ordered quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Currency of the unit price.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Unit of measure.</summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>Domain of the first classification.</summary>
    public string? ClassificationDomain { get; set; }

    /// <summary>Value of the first classification.</summary>
    public string? Classification { get; set; }

    /// <summary>Manufacturer part number.</summary>
    public string? ManufacturerPartId { get; set; }

    /// <summary>Manufacturer name.</summary>
    public string? ManufacturerName { get; set; }

    /// <summary>The <c>requestedDeliveryDate</c> as received.</summary>
    public string? RequestedDeliveryDate { get; set; }

    /// <summary>Quantity multiplied by unit price.</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
