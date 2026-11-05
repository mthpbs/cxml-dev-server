using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>Detailed item data (price, descriptions, classification, manufacturer, ...).</summary>
public sealed class ItemDetail
{
    /// <summary>The unit price.</summary>
    public Money UnitPrice { get; set; } = new();

    /// <summary>One or more descriptions.</summary>
    public List<Description> Descriptions { get; set; } = [];

    /// <summary>Unit of measure code (UN/ECE Rec. 20), e.g. <c>EA</c>.</summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>Classifications; the DTD requires at least one when sending <c>ItemIn</c>.</summary>
    public List<Classification> Classifications { get; set; } = [];

    /// <summary>Manufacturer part number.</summary>
    public string? ManufacturerPartId { get; set; }

    /// <summary>Manufacturer name.</summary>
    public string? ManufacturerName { get; set; }

    /// <summary>Product page URL.</summary>
    public string? Url { get; set; }

    /// <summary>Lead time in days.</summary>
    public int? LeadTime { get; set; }

    /// <summary>Extrinsic name/value pairs.</summary>
    public List<Extrinsic> Extrinsics { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static ItemDetail Parse(XElement e) => new()
    {
        UnitPrice = Money.ParseWrapped(e.Element("UnitPrice")) ?? new Money(),
        Descriptions = e.Elements("Description").Select(Description.Parse).ToList(),
        UnitOfMeasure = e.ChildText("UnitOfMeasure") ?? string.Empty,
        Classifications = e.Elements("Classification").Select(Classification.Parse).ToList(),
        ManufacturerPartId = e.ChildText("ManufacturerPartID"),
        ManufacturerName = e.ChildText("ManufacturerName"),
        Url = e.ChildText("URL"),
        LeadTime = e.ChildText("LeadTime").ToIntOrNull(),
        Extrinsics = Extrinsic.ParseAll(e),
        Raw = e,
    };

    internal XElement ToXml()
    {
        // Order follows the DTD: UnitPrice, Description+, UnitOfMeasure, Classification+,
        // ManufacturerPartID?, ManufacturerName?, URL?, LeadTime?, Extrinsic*.
        var e = new XElement("ItemDetail", UnitPrice.Wrap("UnitPrice"));
        e.Add(Descriptions.Select(d => d.ToXml()));
        e.Add(new XElement("UnitOfMeasure", UnitOfMeasure));
        e.Add(Classifications.Select(c => c.ToXml()));
        if (!string.IsNullOrEmpty(ManufacturerPartId))
        {
            e.Add(new XElement("ManufacturerPartID", ManufacturerPartId));
        }

        if (!string.IsNullOrEmpty(ManufacturerName))
        {
            e.Add(new XElement("ManufacturerName", ManufacturerName));
        }

        if (!string.IsNullOrEmpty(Url))
        {
            e.Add(new XElement("URL", Url));
        }

        if (LeadTime is { } lead)
        {
            e.Add(new XElement("LeadTime", lead.ToInvariantString()));
        }

        e.Add(Extrinsics.Select(x => x.ToXml()));
        return e;
    }
}
