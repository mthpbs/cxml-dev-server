using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A party address: <c>Name, PostalAddress?, Email?, Phone?, Fax?, URL?</c>.</summary>
public sealed class Address
{
    /// <summary>Display name of the location or organisation.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary><c>xml:lang</c> of <see cref="Name"/> (required by the DTD).</summary>
    public string NameLang { get; set; } = "en";

    /// <summary>The postal address.</summary>
    public PostalAddress? PostalAddress { get; set; }

    /// <summary>E-mail address.</summary>
    public string? Email { get; set; }

    /// <summary>Telephone number.</summary>
    public Phone? Phone { get; set; }

    /// <summary>Web address.</summary>
    public string? Url { get; set; }

    /// <summary>Optional <c>addressID</c> attribute.</summary>
    public string? AddressId { get; set; }

    /// <summary>Optional <c>addressIDDomain</c> attribute.</summary>
    public string? AddressIdDomain { get; set; }

    /// <summary>Optional <c>isoCountryCode</c> attribute.</summary>
    public string? IsoCountryCode { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static Address Parse(XElement e) => new()
    {
        Name = e.ChildText("Name") ?? string.Empty,
        NameLang = e.Element("Name")?.LangAttr() ?? "en",
        PostalAddress = e.Element("PostalAddress") is { } p ? Common.PostalAddress.Parse(p) : null,
        Email = e.ChildText("Email"),
        Phone = e.Element("Phone") is { } ph ? Common.Phone.Parse(ph) : null,
        Url = e.ChildText("URL"),
        AddressId = e.Attr("addressID"),
        AddressIdDomain = e.Attr("addressIDDomain"),
        IsoCountryCode = e.Attr("isoCountryCode"),
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("Address");
        e.AddAttr("isoCountryCode", IsoCountryCode);
        e.AddAttr("addressID", AddressId);
        e.AddAttr("addressIDDomain", AddressIdDomain);
        e.Add(new XElement("Name", new XAttribute(XmlNames.Lang, NameLang), Name));
        e.AddIfNotNull(PostalAddress?.ToXml());
        if (!string.IsNullOrEmpty(Email))
        {
            e.Add(new XElement("Email", Email));
        }

        e.AddIfNotNull(Phone?.ToXml());
        if (!string.IsNullOrEmpty(Url))
        {
            e.Add(new XElement("URL", Url));
        }

        return e;
    }
}
