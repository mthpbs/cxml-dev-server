using System.Xml.Linq;

namespace Cxml.Protocol.Common;

/// <summary>The <c>ShipTo</c> wrapper around an <see cref="Common.Address"/>.</summary>
public sealed class ShipTo
{
    /// <summary>The delivery address.</summary>
    public Address Address { get; set; } = new();

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static ShipTo Parse(XElement e) => new()
    {
        Address = e.Element("Address") is { } a ? Address.Parse(a) : new Address(),
        Raw = e,
    };

    internal XElement ToXml() => new("ShipTo", Address.ToXml());
}

/// <summary>The <c>BillTo</c> wrapper around an <see cref="Common.Address"/>.</summary>
public sealed class BillTo
{
    /// <summary>The invoice address.</summary>
    public Address Address { get; set; } = new();

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static BillTo Parse(XElement e) => new()
    {
        Address = e.Element("Address") is { } a ? Address.Parse(a) : new Address(),
        Raw = e,
    };

    internal XElement ToXml() => new("BillTo", Address.ToXml());
}
