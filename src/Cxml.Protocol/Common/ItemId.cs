using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>Identifies an item: <c>SupplierPartID, SupplierPartAuxiliaryID?, BuyerPartID?</c>.</summary>
public sealed class ItemId
{
    /// <summary>The supplier's part number.</summary>
    public string SupplierPartId { get; set; } = string.Empty;

    /// <summary>
    /// The <c>SupplierPartAuxiliaryID</c> as inner markup (content is <c>ANY</c>; plain text is the
    /// common case). Used by suppliers to carry a cookie to rebuild a cart on edit.
    /// </summary>
    public string? SupplierPartAuxiliaryId { get; set; }

    /// <summary>The buyer's part number.</summary>
    public string? BuyerPartId { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static ItemId Parse(XElement e) => new()
    {
        SupplierPartId = e.ChildText("SupplierPartID") ?? string.Empty,
        SupplierPartAuxiliaryId = e.Element("SupplierPartAuxiliaryID")?.InnerXml(),
        BuyerPartId = e.ChildText("BuyerPartID"),
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("ItemID", new XElement("SupplierPartID", SupplierPartId));
        if (SupplierPartAuxiliaryId is not null)
        {
            e.Add(new XElement("SupplierPartAuxiliaryID", XmlExtensions.ParseInnerXml(SupplierPartAuxiliaryId)));
        }

        if (!string.IsNullOrEmpty(BuyerPartId))
        {
            e.Add(new XElement("BuyerPartID", BuyerPartId));
        }

        return e;
    }
}
