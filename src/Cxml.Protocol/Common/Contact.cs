using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A contact person or group: <c>Name, PostalAddress*, Email*, Phone*, ... Extrinsic*</c>.</summary>
public sealed class Contact
{
    /// <summary>Role such as <c>endUser</c> or <c>buyer</c>.</summary>
    public string? Role { get; set; }

    /// <summary>Contact name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary><c>xml:lang</c> of <see cref="Name"/>.</summary>
    public string NameLang { get; set; } = "en";

    /// <summary>Postal addresses.</summary>
    public List<PostalAddress> PostalAddresses { get; set; } = [];

    /// <summary>E-mail addresses.</summary>
    public List<string> Emails { get; set; } = [];

    /// <summary>Phone numbers.</summary>
    public List<Phone> Phones { get; set; } = [];

    /// <summary>Web addresses.</summary>
    public List<string> Urls { get; set; } = [];

    /// <summary>Extrinsic name/value pairs (commonly <c>UserEmail</c>, <c>UniqueName</c>).</summary>
    public List<Extrinsic> Extrinsics { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static Contact Parse(XElement e) => new()
    {
        Role = e.Attr("role"),
        Name = e.ChildText("Name") ?? string.Empty,
        NameLang = e.Element("Name")?.LangAttr() ?? "en",
        PostalAddresses = e.Elements("PostalAddress").Select(PostalAddress.Parse).ToList(),
        Emails = e.Elements("Email").Select(x => x.Value.Trim()).ToList(),
        Phones = e.Elements("Phone").Select(Phone.Parse).ToList(),
        Urls = e.Elements("URL").Select(x => x.Value.Trim()).ToList(),
        Extrinsics = Extrinsic.ParseAll(e),
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("Contact");
        e.AddAttr("role", Role);
        e.Add(new XElement("Name", new XAttribute(XmlNames.Lang, NameLang), Name));
        e.Add(PostalAddresses.Select(p => p.ToXml()));
        e.Add(Emails.Select(x => new XElement("Email", x)));
        e.Add(Phones.Select(p => p.ToXml()));
        e.Add(Urls.Select(x => new XElement("URL", x)));
        e.Add(Extrinsics.Select(x => x.ToXml()));
        return e;
    }
}
