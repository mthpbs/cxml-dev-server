using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Documents.Profile;

/// <summary>Describes the transactions a cXML server supports and where to send them.</summary>
public sealed class ProfileResponse : ICxmlPayload
{
    /// <summary>The element name, <c>ProfileResponse</c>.</summary>
    public const string Name = "ProfileResponse";

    internal static readonly CxmlPayloadCodec<ProfileResponse> Codec = new(Name, Parse, p => p.ToXml());

    /// <inheritdoc />
    public string ElementName => Name;

    /// <summary>When these services became available.</summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>When the service information was last refreshed.</summary>
    public DateTimeOffset? LastRefresh { get; set; }

    /// <summary>Profile-wide options.</summary>
    public List<ProfileOption> Options { get; set; } = [];

    /// <summary>Supported transactions.</summary>
    public List<ProfileTransaction> Transactions { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    private static ProfileResponse Parse(XElement e) => new()
    {
        EffectiveDate = CxmlTimestamp.ParseOrNull(e.Attr("effectiveDate")) ?? default,
        LastRefresh = CxmlTimestamp.ParseOrNull(e.Attr("lastRefresh")),
        Options = e.Elements("Option").Select(ProfileOption.Parse).ToList(),
        Transactions = e.Elements("Transaction").Select(ProfileTransaction.Parse).ToList(),
        Raw = e,
    };

    private XElement ToXml()
    {
        var e = new XElement(Name, new XAttribute("effectiveDate", CxmlTimestamp.Format(EffectiveDate)));
        if (LastRefresh is { } refresh)
        {
            e.Add(new XAttribute("lastRefresh", CxmlTimestamp.Format(refresh)));
        }

        e.Add(Options.Select(o => o.ToXml()));
        e.Add(Transactions.Select(t => t.ToXml()));
        return e;
    }
}

/// <summary>A profile <c>Option</c> name/value pair.</summary>
public sealed class ProfileOption
{
    /// <summary>The option name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The option value.</summary>
    public string Value { get; set; } = string.Empty;

    internal static ProfileOption Parse(XElement e) => new()
    {
        Name = e.Attr("name") ?? string.Empty,
        Value = e.Value.Trim(),
    };

    internal XElement ToXml() => new("Option", new XAttribute("name", Name), Value);
}

/// <summary>A supported transaction (<c>requestName</c>) and the URL that accepts it.</summary>
public sealed class ProfileTransaction
{
    /// <summary>The request element name, e.g. <c>OrderRequest</c>.</summary>
    public string RequestName { get; set; } = string.Empty;

    /// <summary>The URL that accepts the request.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Transaction-specific options.</summary>
    public List<ProfileOption> Options { get; set; } = [];

    internal static ProfileTransaction Parse(XElement e) => new()
    {
        RequestName = e.Attr("requestName") ?? string.Empty,
        Url = e.ChildText("URL") ?? string.Empty,
        Options = e.Elements("Option").Select(ProfileOption.Parse).ToList(),
    };

    internal XElement ToXml()
    {
        var e = new XElement("Transaction",
            new XAttribute("requestName", RequestName),
            new XElement("URL", Url));
        e.Add(Options.Select(o => o.ToXml()));
        return e;
    }
}
