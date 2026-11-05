namespace CxmlDevServer.Core.Domain;

/// <summary>A product offered in the PunchOut shop.</summary>
public class CatalogItem : IAuditedEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The supplier's part number (<c>SupplierPartID</c>); unique.</summary>
    public string SupplierPartId { get; set; } = string.Empty;

    /// <summary>Optional fixed <c>SupplierPartAuxiliaryID</c>; when empty a <c>cxmldev:{id}</c> cookie is used.</summary>
    public string? SupplierPartAuxiliaryId { get; set; }

    /// <summary>Short product name.</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>Product description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Price per unit.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>ISO 4217 currency of <see cref="UnitPrice"/>.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Unit of measure (UN/CEFACT code, e.g. <c>EA</c>).</summary>
    public string UnitOfMeasure { get; set; } = "EA";

    /// <summary>UNSPSC classification code.</summary>
    public string? Unspsc { get; set; }

    /// <summary>Manufacturer name.</summary>
    public string? ManufacturerName { get; set; }

    /// <summary>Manufacturer part number.</summary>
    public string? ManufacturerPartId { get; set; }

    /// <summary>Lead time in days.</summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>Product image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Catalog category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Inactive items are hidden from the shop.</summary>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
