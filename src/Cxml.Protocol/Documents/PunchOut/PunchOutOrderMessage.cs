using System.Xml.Linq;
using Cxml.Protocol.Common;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Documents.PunchOut;

/// <summary>The shopping cart the supplier hands back to the buyer at the end of a PunchOut session.</summary>
public sealed class PunchOutOrderMessage : ICxmlPayload
{
    /// <summary>The element name, <c>PunchOutOrderMessage</c>.</summary>
    public const string Name = "PunchOutOrderMessage";

    internal static readonly CxmlPayloadCodec<PunchOutOrderMessage> Codec = new(Name, Parse, p => p.ToXml());

    /// <inheritdoc />
    public string ElementName => Name;

    /// <summary>The <c>BuyerCookie</c> from the setup request, as inner markup, echoed unchanged.</summary>
    public string BuyerCookie { get; set; } = string.Empty;

    /// <summary>The cart header.</summary>
    public PunchOutOrderMessageHeader Header { get; set; } = new();

    /// <summary>The cart lines.</summary>
    public List<ItemIn> Items { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    private static PunchOutOrderMessage Parse(XElement e) => new()
    {
        BuyerCookie = e.Element("BuyerCookie")?.InnerXml() ?? string.Empty,
        Header = e.Element("PunchOutOrderMessageHeader") is { } h ? PunchOutOrderMessageHeader.Parse(h) : new PunchOutOrderMessageHeader(),
        Items = e.Elements("ItemIn").Select(ItemIn.Parse).ToList(),
        Raw = e,
    };

    private XElement ToXml()
    {
        var e = new XElement(Name,
            new XElement("BuyerCookie", XmlExtensions.ParseInnerXml(BuyerCookie)),
            Header.ToXml());
        e.Add(Items.Select(i => i.ToXml()));
        return e;
    }
}

/// <summary>The <c>PunchOutOrderMessageHeader</c>: allowed follow-up operation, total, and cart-level ship-to/shipping/tax.</summary>
public sealed class PunchOutOrderMessageHeader
{
    /// <summary>Highest follow-up operation the buyer may perform (create, inspect or edit).</summary>
    public PunchOutOperation OperationAllowed { get; set; } = PunchOutOperation.Create;

    /// <summary>Optional <c>quoteStatus</c> (<c>pending</c> or <c>final</c>).</summary>
    public string? QuoteStatus { get; set; }

    /// <summary>Cart total.</summary>
    public Money Total { get; set; } = new();

    /// <summary>Cart-level ship-to.</summary>
    public ShipTo? ShipTo { get; set; }

    /// <summary>Cart-level shipping.</summary>
    public Shipping? Shipping { get; set; }

    /// <summary>Cart-level tax.</summary>
    public Tax? Tax { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static PunchOutOrderMessageHeader Parse(XElement e) => new()
    {
        OperationAllowed = PunchOutOperationExtensions.ParseOperation(e.Attr("operationAllowed")),
        QuoteStatus = e.Attr("quoteStatus"),
        Total = Money.ParseWrapped(e.Element("Total")) ?? new Money(),
        ShipTo = e.Element("ShipTo") is { } s ? Common.ShipTo.Parse(s) : null,
        Shipping = e.Element("Shipping") is { } sh ? Common.Shipping.Parse(sh) : null,
        Tax = e.Element("Tax") is { } t ? Common.Tax.Parse(t) : null,
        Raw = e,
    };

    internal XElement ToXml()
    {
        // DTD order: SourcingStatus?, Total, ShipTo?, Shipping?, Tax?
        var e = new XElement("PunchOutOrderMessageHeader",
            new XAttribute("operationAllowed", OperationAllowed.ToCxml()));
        e.AddAttr("quoteStatus", QuoteStatus);
        e.Add(Total.Wrap("Total"));
        e.AddIfNotNull(ShipTo?.ToXml());
        e.AddIfNotNull(Shipping?.ToXml());
        e.AddIfNotNull(Tax?.ToXml());
        return e;
    }
}
