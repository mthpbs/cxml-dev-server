using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>An accounting distribution: an <c>Accounting</c> element and the <c>Charge</c> against it.</summary>
public sealed class Distribution
{
    /// <summary>The <c>Accounting/@name</c> attribute.</summary>
    public string AccountingName { get; set; } = string.Empty;

    /// <summary>Accounting segments (e.g. cost center, GL account).</summary>
    public List<AccountingSegment> Segments { get; set; } = [];

    /// <summary>The amount charged.</summary>
    public Money Charge { get; set; } = new();

    internal static Distribution Parse(XElement e)
    {
        var accounting = e.Element("Accounting");
        return new Distribution
        {
            AccountingName = accounting?.Attr("name") ?? string.Empty,
            Segments = accounting?.Elements().Select(AccountingSegment.Parse).ToList() ?? [],
            Charge = Money.ParseWrapped(e.Element("Charge")) ?? new Money(),
        };
    }

    internal XElement ToXml() => new("Distribution",
        new XElement("Accounting",
            new XAttribute("name", AccountingName),
            Segments.Select(s => s.ToXml())),
        Charge.Wrap("Charge"));
}

/// <summary>
/// An accounting segment. When <see cref="Type"/> is set it is the legacy empty <c>Segment</c> element
/// (<c>type/id/description</c> attributes); otherwise it is an <c>AccountingSegment</c> with Name and Description.
/// </summary>
public sealed class AccountingSegment
{
    /// <summary>The segment id (key).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Segment name (AccountingSegment form).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Segment description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The <c>xml:lang</c> for Name and Description.</summary>
    public string Lang { get; set; } = "en";

    /// <summary>Set to use the legacy <c>Segment</c> element with this <c>type</c>.</summary>
    public string? Type { get; set; }

    internal static AccountingSegment Parse(XElement e) => e.Name.LocalName == "Segment"
        ? new AccountingSegment
        {
            Type = e.Attr("type") ?? string.Empty,
            Id = e.Attr("id") ?? string.Empty,
            Description = e.Attr("description") ?? string.Empty,
        }
        : new AccountingSegment
        {
            Id = e.Attr("id") ?? string.Empty,
            Name = e.ChildText("Name") ?? string.Empty,
            Description = e.ChildText("Description") ?? string.Empty,
            Lang = e.Element("Name")?.LangAttr() ?? "en",
        };

    internal XElement ToXml() => Type is not null
        ? new XElement("Segment",
            new XAttribute("type", Type),
            new XAttribute("id", Id),
            new XAttribute("description", Description))
        : new XElement("AccountingSegment",
            new XAttribute("id", Id),
            new XElement("Name", new XAttribute(XmlNames.Lang, Lang), Name),
            new XElement("Description", new XAttribute(XmlNames.Lang, Lang), Description));
}
