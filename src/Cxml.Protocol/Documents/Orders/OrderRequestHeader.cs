using System.Xml.Linq;
using Cxml.Protocol.Common;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Documents.Orders;

/// <summary>The header of an <see cref="OrderRequest"/>.</summary>
public sealed class OrderRequestHeader
{
    /// <summary>The buyer's order number.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>The order date exactly as received (use <see cref="OrderDate"/> for the parsed value).</summary>
    public string? OrderDateText { get; set; }

    /// <summary>The parsed order date, or <c>null</c> if missing or unparseable.</summary>
    public DateTimeOffset? OrderDate { get; set; }

    /// <summary>New, update or delete.</summary>
    public OrderRequestType Type { get; set; }

    /// <summary>The <c>orderVersion</c> attribute.</summary>
    public int? OrderVersion { get; set; }

    /// <summary>The <c>orderType</c> attribute (<c>regular</c>, <c>release</c>, <c>blanket</c>, ...).</summary>
    public string? OrderType { get; set; }

    /// <summary>Order total.</summary>
    public Money Total { get; set; } = new();

    /// <summary>Order-level ship-to.</summary>
    public ShipTo? ShipTo { get; set; }

    /// <summary>Bill-to (required by the DTD).</summary>
    public BillTo? BillTo { get; set; }

    /// <summary>Shipping charge.</summary>
    public Shipping? Shipping { get; set; }

    /// <summary>Tax.</summary>
    public Tax? Tax { get; set; }

    /// <summary>Payment method.</summary>
    public Payment? Payment { get; set; }

    /// <summary>Contacts.</summary>
    public List<Contact> Contacts { get; set; } = [];

    /// <summary>Comments.</summary>
    public List<Comments> Comments { get; set; } = [];

    /// <summary><c>DocumentReference@payloadID</c>, e.g. the PunchOut order message this order derives from.</summary>
    public string? DocumentReferencePayloadId { get; set; }

    /// <summary>Extrinsic name/value pairs.</summary>
    public List<Extrinsic> Extrinsics { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static OrderRequestHeader Parse(XElement e)
    {
        var dateText = e.Attr("orderDate");
        return new OrderRequestHeader
        {
            OrderId = e.Attr("orderID") ?? string.Empty,
            OrderDateText = dateText,
            OrderDate = CxmlTimestamp.ParseOrNull(dateText),
            Type = OrderRequestTypeExtensions.Parse(e.Attr("type")),
            OrderVersion = e.Attr("orderVersion").ToIntOrNull(),
            OrderType = e.Attr("orderType"),
            Total = Money.ParseWrapped(e.Element("Total")) ?? new Money(),
            ShipTo = e.Element("ShipTo") is { } s ? Common.ShipTo.Parse(s) : null,
            BillTo = e.Element("BillTo") is { } b ? Common.BillTo.Parse(b) : null,
            Shipping = e.Element("Shipping") is { } sh ? Common.Shipping.Parse(sh) : null,
            Tax = e.Element("Tax") is { } t ? Common.Tax.Parse(t) : null,
            Payment = e.Element("Payment") is { } p ? Common.Payment.Parse(p) : null,
            Contacts = e.Elements("Contact").Select(Contact.Parse).ToList(),
            Comments = e.Elements("Comments").Select(Common.Comments.Parse).ToList(),
            DocumentReferencePayloadId = e.Element("DocumentReference")?.Attr("payloadID"),
            Extrinsics = Extrinsic.ParseAll(e),
            Raw = e,
        };
    }

    internal XElement ToXml()
    {
        var e = new XElement("OrderRequestHeader", new XAttribute("orderID", OrderId));
        var date = OrderDate is { } d ? CxmlTimestamp.Format(d) : OrderDateText;
        e.AddAttr("orderDate", date);
        e.AddAttr("orderType", OrderType);
        e.Add(new XAttribute("type", Type.ToCxml()));
        if (OrderVersion is { } v)
        {
            e.Add(new XAttribute("orderVersion", v.ToInvariantString()));
        }

        // DTD order: Total, ShipTo?, BillTo, Shipping?, Tax?, Payment?, Contact*, Comments*, DocumentReference?, Extrinsic*
        e.Add(Total.Wrap("Total"));
        e.AddIfNotNull(ShipTo?.ToXml());
        e.AddIfNotNull(BillTo?.ToXml());
        e.AddIfNotNull(Shipping?.ToXml());
        e.AddIfNotNull(Tax?.ToXml());
        e.AddIfNotNull(Payment?.ToXml());
        e.Add(Contacts.Select(c => c.ToXml()));
        e.Add(Comments.Select(c => c.ToXml()));
        if (!string.IsNullOrEmpty(DocumentReferencePayloadId))
        {
            e.Add(new XElement("DocumentReference", new XAttribute("payloadID", DocumentReferencePayloadId)));
        }

        e.Add(Extrinsics.Select(x => x.ToXml()));
        return e;
    }
}
