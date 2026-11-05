using System.Xml.Linq;
using Cxml.Protocol.Common;

namespace Cxml.Protocol.Documents.Orders;

/// <summary>A purchase order sent by the buyer: header plus one or more <see cref="ItemOut"/> lines.</summary>
public sealed class OrderRequest : ICxmlPayload
{
    /// <summary>The element name, <c>OrderRequest</c>.</summary>
    public const string Name = "OrderRequest";

    internal static readonly CxmlPayloadCodec<OrderRequest> Codec = new(Name, Parse, p => p.ToXml());

    /// <inheritdoc />
    public string ElementName => Name;

    /// <summary>The order header.</summary>
    public OrderRequestHeader Header { get; set; } = new();

    /// <summary>The order lines (the DTD requires at least one).</summary>
    public List<ItemOut> Items { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    private static OrderRequest Parse(XElement e) => new()
    {
        Header = e.Element("OrderRequestHeader") is { } h ? OrderRequestHeader.Parse(h) : new OrderRequestHeader(),
        Items = e.Elements("ItemOut").Select(ItemOut.Parse).ToList(),
        Raw = e,
    };

    private XElement ToXml()
    {
        var e = new XElement(Name, Header.ToXml());
        e.Add(Items.Select(i => i.ToXml()));
        return e;
    }
}
