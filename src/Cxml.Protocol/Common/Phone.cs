using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A named telephone number (<c>Phone/TelephoneNumber</c>).</summary>
public sealed class Phone
{
    /// <summary>Optional name of the number, e.g. <c>work</c>.</summary>
    public string? Name { get; set; }

    /// <summary>International dial code, e.g. <c>1</c>.</summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>ISO 3166 country code of the dial code.</summary>
    public string IsoCountryCode { get; set; } = string.Empty;

    /// <summary>Area or city code.</summary>
    public string AreaOrCityCode { get; set; } = string.Empty;

    /// <summary>Local number.</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Optional extension.</summary>
    public string? Extension { get; set; }

    internal static Phone Parse(XElement e)
    {
        var tel = e.Element("TelephoneNumber");
        var cc = tel?.Element("CountryCode");
        return new Phone
        {
            Name = e.Attr("name"),
            CountryCode = cc?.Value.Trim() ?? string.Empty,
            IsoCountryCode = cc?.Attr("isoCountryCode") ?? string.Empty,
            AreaOrCityCode = tel?.ChildText("AreaOrCityCode") ?? string.Empty,
            Number = tel?.ChildText("Number") ?? string.Empty,
            Extension = tel?.ChildText("Extension"),
        };
    }

    internal XElement ToXml()
    {
        var tel = new XElement("TelephoneNumber",
            new XElement("CountryCode", new XAttribute("isoCountryCode", IsoCountryCode), CountryCode),
            new XElement("AreaOrCityCode", AreaOrCityCode),
            new XElement("Number", Number));
        if (!string.IsNullOrEmpty(Extension))
        {
            tel.Add(new XElement("Extension", Extension));
        }

        var e = new XElement("Phone", tel);
        e.AddAttr("name", Name);
        return e;
    }
}
