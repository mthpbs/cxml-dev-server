using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A requested line item, used in <c>OrderRequest</c> and <c>PunchOutSetupRequest</c>.</summary>
public sealed class ItemOut
{
    /// <summary>The requested quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>The 1-based line number, when given.</summary>
    public int? LineNumber { get; set; }

    /// <summary>Requested delivery date as received (<c>yyyy-MM-dd</c> or a date-time).</summary>
    public string? RequestedDeliveryDate { get; set; }

    /// <summary>True when the line is an ad-hoc (non-catalog) item.</summary>
    public bool IsAdHoc { get; set; }

    /// <summary>The item identifiers.</summary>
    public ItemId ItemId { get; set; } = new();

    /// <summary>Item detail; optional for <c>ItemOut</c>.</summary>
    public ItemDetail? ItemDetail { get; set; }

    /// <summary>Line-level ship-to.</summary>
    public ShipTo? ShipTo { get; set; }

    /// <summary>Line-level shipping.</summary>
    public Shipping? Shipping { get; set; }

    /// <summary>Line-level tax.</summary>
    public Tax? Tax { get; set; }

    /// <summary>Accounting distributions.</summary>
    public List<Distribution> Distributions { get; set; } = [];

    /// <summary>Line-level contacts.</summary>
    public List<Contact> Contacts { get; set; } = [];

    /// <summary>Line-level comments.</summary>
    public List<Comments> Comments { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static ItemOut Parse(XElement e) => new()
    {
        Quantity = e.Attr("quantity").ToDecimalOrNull() ?? 0m,
        LineNumber = e.Attr("lineNumber").ToIntOrNull(),
        RequestedDeliveryDate = e.Attr("requestedDeliveryDate"),
        IsAdHoc = string.Equals(e.Attr("isAdHoc"), "yes", StringComparison.OrdinalIgnoreCase),
        ItemId = e.Element("ItemID") is { } id ? ItemId.Parse(id) : new ItemId(),
        ItemDetail = e.Element("ItemDetail") is { } d ? ItemDetail.Parse(d) : null,
        ShipTo = e.Element("ShipTo") is { } s ? ShipTo.Parse(s) : null,
        Shipping = e.Element("Shipping") is { } sh ? Shipping.Parse(sh) : null,
        Tax = e.Element("Tax") is { } t ? Tax.Parse(t) : null,
        Distributions = e.Elements("Distribution").Select(Distribution.Parse).ToList(),
        Contacts = e.Elements("Contact").Select(Contact.Parse).ToList(),
        Comments = e.Elements("Comments").Select(Common.Comments.Parse).ToList(),
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("ItemOut", new XAttribute("quantity", Quantity.ToInvariantString()));
        if (LineNumber is { } line)
        {
            e.Add(new XAttribute("lineNumber", line.ToInvariantString()));
        }

        e.AddAttr("requestedDeliveryDate", RequestedDeliveryDate);
        if (IsAdHoc)
        {
            e.Add(new XAttribute("isAdHoc", "yes"));
        }

        // Order follows the DTD: ItemID, ItemDetail?, ShipTo?, Shipping?, Tax?, Distribution*, Contact*, Comments*.
        e.Add(ItemId.ToXml());
        e.AddIfNotNull(ItemDetail?.ToXml());
        e.AddIfNotNull(ShipTo?.ToXml());
        e.AddIfNotNull(Shipping?.ToXml());
        e.AddIfNotNull(Tax?.ToXml());
        e.Add(Distributions.Select(d => d.ToXml()));
        e.Add(Contacts.Select(c => c.ToXml()));
        e.Add(Comments.Select(c => c.ToXml()));
        return e;
    }
}
