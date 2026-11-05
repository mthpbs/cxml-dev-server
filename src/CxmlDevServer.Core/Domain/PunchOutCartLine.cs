namespace CxmlDevServer.Core.Domain;

/// <summary>One line of a PunchOut cart.</summary>
public class PunchOutCartLine
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Owning session.</summary>
    public int SessionId { get; set; }

    /// <summary>Navigation to <see cref="SessionId"/>.</summary>
    public PunchOutSession? Session { get; set; }

    /// <summary>1-based line number in the cart.</summary>
    public int LineNumber { get; set; }

    /// <summary>The catalog item, or <c>null</c> for ad-hoc lines copied from an edit/inspect request.</summary>
    public int? CatalogItemId { get; set; }

    /// <summary>Navigation to <see cref="CatalogItemId"/>.</summary>
    public CatalogItem? CatalogItem { get; set; }

    /// <summary>Supplier part number.</summary>
    public string SupplierPartId { get; set; } = string.Empty;

    /// <summary>Supplier part auxiliary id cookie (inner XML).</summary>
    public string? SupplierPartAuxiliaryId { get; set; }

    /// <summary>Short name.</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>Description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ordered quantity.</summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Currency of <see cref="UnitPrice"/>.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Unit of measure.</summary>
    public string UnitOfMeasure { get; set; } = "EA";

    /// <summary>UNSPSC code (or the value of the classification the buyer sent for an ad-hoc line).</summary>
    public string? Unspsc { get; set; }

    /// <summary>Domain of <see cref="Unspsc"/>; <c>null</c> means <c>UNSPSC</c>.</summary>
    public string? ClassificationDomain { get; set; }

    /// <summary>Manufacturer name.</summary>
    public string? ManufacturerName { get; set; }

    /// <summary>Manufacturer part number.</summary>
    public string? ManufacturerPartId { get; set; }

    /// <summary>Lead time in days.</summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>True when the line is not backed by a catalog item.</summary>
    public bool IsAdHoc { get; set; }

    /// <summary>Quantity multiplied by unit price.</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
