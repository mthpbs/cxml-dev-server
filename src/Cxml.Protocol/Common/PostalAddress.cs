using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A postal address block: <c>DeliverTo*, Street+, City, State?, PostalCode?, Country</c>.</summary>
public sealed class PostalAddress
{
    /// <summary>Optional name of the postal address, e.g. <c>default</c>.</summary>
    public string? Name { get; set; }

    /// <summary>Recipient lines.</summary>
    public List<string> DeliverTo { get; set; } = [];

    /// <summary>Street lines.</summary>
    public List<string> Street { get; set; } = [];

    /// <summary>City name.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>State or province.</summary>
    public string? State { get; set; }

    /// <summary>Postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Human-readable country name.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>ISO 3166 two-letter country code (<c>Country/@isoCountryCode</c>).</summary>
    public string IsoCountryCode { get; set; } = string.Empty;

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static PostalAddress Parse(XElement e) => new()
    {
        Name = e.Attr("name"),
        DeliverTo = e.Elements("DeliverTo").Select(x => x.Value.Trim()).ToList(),
        Street = e.Elements("Street").Select(x => x.Value.Trim()).ToList(),
        City = e.ChildText("City") ?? string.Empty,
        State = e.ChildText("State"),
        PostalCode = e.ChildText("PostalCode"),
        Country = e.ChildText("Country") ?? string.Empty,
        IsoCountryCode = e.Element("Country")?.Attr("isoCountryCode") ?? string.Empty,
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("PostalAddress");
        e.AddAttr("name", Name);
        e.Add(DeliverTo.Select(d => new XElement("DeliverTo", d)));
        e.Add(Street.Select(s => new XElement("Street", s)));
        e.Add(new XElement("City", City));
        if (!string.IsNullOrEmpty(State))
        {
            e.Add(new XElement("State", State));
        }

        if (!string.IsNullOrEmpty(PostalCode))
        {
            e.Add(new XElement("PostalCode", PostalCode));
        }

        e.Add(new XElement("Country", new XAttribute("isoCountryCode", IsoCountryCode), Country));
        return e;
    }
}
