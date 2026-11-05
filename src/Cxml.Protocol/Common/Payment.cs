using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>Payment method of an order. Only purchasing cards (<c>PCard</c>) are modelled.</summary>
public sealed class Payment
{
    /// <summary>The purchasing card, if the payment is by PCard.</summary>
    public PCard? PCard { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static Payment Parse(XElement e) => new()
    {
        PCard = e.Element("PCard") is { } p ? Common.PCard.Parse(p) : null,
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("Payment");
        e.AddIfNotNull(PCard?.ToXml());
        return e;
    }
}

/// <summary>A purchasing card.</summary>
public sealed class PCard
{
    /// <summary>Card number.</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Expiration date (<c>yyyy-MM-dd</c>).</summary>
    public string Expiration { get; set; } = string.Empty;

    /// <summary>Name on the card.</summary>
    public string? Name { get; set; }

    internal static PCard Parse(XElement e) => new()
    {
        Number = e.Attr("number") ?? string.Empty,
        Expiration = e.Attr("expiration") ?? string.Empty,
        Name = e.Attr("name"),
    };

    internal XElement ToXml()
    {
        var e = new XElement("PCard",
            new XAttribute("number", Number),
            new XAttribute("expiration", Expiration));
        e.AddAttr("name", Name);
        return e;
    }
}
