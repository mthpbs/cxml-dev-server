using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A line item returned to the buyer in a <c>PunchOutOrderMessage</c>.</summary>
public sealed class ItemIn
{
    /// <summary>The quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>The 1-based line number, when given.</summary>
    public int? LineNumber { get; set; }

    /// <summary>The item identifiers.</summary>
    public ItemId ItemId { get; set; } = new();

    /// <summary>Item detail (required by the DTD).</summary>
    public ItemDetail ItemDetail { get; set; } = new();

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

    /// <summary>Line-level comments (the DTD allows at most one).</summary>
    public Comments? Comments { get; set; }

    /// <summary>Extrinsic name/value pairs.</summary>
    public List<Extrinsic> Extrinsics { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static ItemIn Parse(XElement e) => new()
    {
        Quantity = e.Attr("quantity").ToDecimalOrNull() ?? 0m,
        LineNumber = e.Attr("lineNumber").ToIntOrNull(),
        ItemId = e.Element("ItemID") is { } id ? ItemId.Parse(id) : new ItemId(),
        ItemDetail = e.Element("ItemDetail") is { } d ? ItemDetail.Parse(d) : new ItemDetail(),
        ShipTo = e.Element("ShipTo") is { } s ? ShipTo.Parse(s) : null,
        Shipping = e.Element("Shipping") is { } sh ? Shipping.Parse(sh) : null,
        Tax = e.Element("Tax") is { } t ? Tax.Parse(t) : null,
        Distributions = e.Elements("Distribution").Select(Distribution.Parse).ToList(),
        Contacts = e.Elements("Contact").Select(Contact.Parse).ToList(),
        Comments = e.Element("Comments") is { } c ? Common.Comments.Parse(c) : null,
        Extrinsics = Extrinsic.ParseAll(e),
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("ItemIn", new XAttribute("quantity", Quantity.ToInvariantString()));
        if (LineNumber is { } line)
        {
            e.Add(new XAttribute("lineNumber", line.ToInvariantString()));
        }

        // Order follows the DTD: ItemID, ItemDetail, ShipTo?, Shipping?, Tax?, Distribution*, Contact*, Comments?, Extrinsic*.
        e.Add(ItemId.ToXml(), ItemDetail.ToXml());
        e.AddIfNotNull(ShipTo?.ToXml());
        e.AddIfNotNull(Shipping?.ToXml());
        e.AddIfNotNull(Tax?.ToXml());
        e.Add(Distributions.Select(d => d.ToXml()));
        e.Add(Contacts.Select(c => c.ToXml()));
        e.AddIfNotNull(Comments?.ToXml());
        e.Add(Extrinsics.Select(x => x.ToXml()));
        return e;
    }
}
