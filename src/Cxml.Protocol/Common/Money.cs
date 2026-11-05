using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A monetary amount: <c>&lt;Money currency="USD"&gt;12.50&lt;/Money&gt;</c>.</summary>
public sealed class Money
{
    /// <summary>Creates an empty amount.</summary>
    public Money() { }

    /// <summary>Creates an amount in the given ISO 4217 currency.</summary>
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>The numeric amount (scale is preserved when writing).</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency code.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Optional amount in <see cref="AlternateCurrency"/>.</summary>
    public string? AlternateAmount { get; set; }

    /// <summary>Optional alternate ISO 4217 currency code.</summary>
    public string? AlternateCurrency { get; set; }

    /// <summary>The element text exactly as received, when parsed from XML.</summary>
    public string? AmountText { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static Money Parse(XElement e) => new()
    {
        Amount = e.Value.ToDecimalOrNull() ?? 0m,
        AmountText = e.Value.Trim(),
        Currency = e.Attr("currency") ?? string.Empty,
        AlternateAmount = e.Attr("alternateAmount"),
        AlternateCurrency = e.Attr("alternateCurrency"),
        Raw = e,
    };

    internal static Money? ParseOptional(XElement? e) => e is null ? null : Parse(e);

    internal XElement ToXml()
    {
        var e = new XElement("Money", new XAttribute("currency", Currency));
        e.AddAttr("alternateAmount", AlternateAmount);
        e.AddAttr("alternateCurrency", AlternateCurrency);
        e.Add(Amount.ToInvariantString());
        return e;
    }

    /// <summary>Wraps this amount in a container element such as <c>Total</c> or <c>UnitPrice</c>.</summary>
    internal XElement Wrap(string containerName) => new(containerName, ToXml());

    /// <summary>Parses the <see cref="Money"/> child of a wrapper element such as <c>Total</c>.</summary>
    internal static Money? ParseWrapped(XElement? wrapper) =>
        wrapper?.Element("Money") is { } m ? Parse(m) : null;
}
