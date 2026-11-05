using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Envelope;

/// <summary>A party identity (<c>Credential</c>) with optional authentication material.</summary>
public sealed class Credential
{
    /// <summary>The <c>type</c> attribute value that marks a marketplace credential.</summary>
    public const string MarketplaceType = "marketplace";

    /// <summary>Identity namespace, e.g. <c>DUNS</c>, <c>NetworkId</c>.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Optional <c>type</c> attribute (only <c>marketplace</c> is defined).</summary>
    public string? Type { get; set; }

    /// <summary>The identity value (whitespace-trimmed).</summary>
    public string Identity { get; set; } = string.Empty;

    /// <summary>Shared secret, when the credential carries one.</summary>
    public string? SharedSecret { get; set; }

    /// <summary>Message authentication code, when the credential carries one.</summary>
    public CredentialMac? CredentialMac { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    /// <summary>True when this credential identifies a marketplace rather than a member company.</summary>
    public bool IsMarketplace => string.Equals(Type, MarketplaceType, StringComparison.OrdinalIgnoreCase);

    internal static Credential Parse(XElement e) => new()
    {
        Domain = e.Attr("domain") ?? string.Empty,
        Type = e.Attr("type"),
        Identity = e.Element("Identity")?.Value.Trim() ?? string.Empty,
        SharedSecret = e.Element("SharedSecret")?.Value,
        CredentialMac = e.Element("CredentialMac") is { } mac ? CredentialMac.Parse(mac) : null,
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("Credential", new XAttribute("domain", Domain));
        if (!string.IsNullOrEmpty(Type))
        {
            e.Add(new XAttribute("type", Type));
        }

        e.Add(new XElement("Identity", Identity));
        if (SharedSecret is not null)
        {
            e.Add(new XElement("SharedSecret", SharedSecret));
        }
        else if (CredentialMac is not null)
        {
            e.Add(CredentialMac.ToXml());
        }

        return e;
    }
}
