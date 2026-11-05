using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Envelope;

/// <summary>A header party (<c>From</c>, <c>To</c>): one or more credentials.</summary>
public class Party
{
    /// <summary>The credentials identifying this party (marketplace and/or member organisation).</summary>
    public List<Credential> Credentials { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    /// <summary>
    /// The credential that identifies the organisation itself: the first non-marketplace credential,
    /// otherwise the first credential. <c>null</c> when there are none.
    /// </summary>
    public Credential? Primary =>
        Credentials.FirstOrDefault(c => !c.IsMarketplace) ?? Credentials.FirstOrDefault();

    /// <summary>Finds the first credential in <paramref name="domain"/> (case-insensitive).</summary>
    public Credential? Find(string domain) =>
        Credentials.FirstOrDefault(c => string.Equals(c.Domain, domain, StringComparison.OrdinalIgnoreCase));

    internal static T Parse<T>(XElement e) where T : Party, new() => new()
    {
        Credentials = e.Elements("Credential").Select(Credential.Parse).ToList(),
        Raw = e,
    };

    internal XElement ToXml(string elementName) => new(elementName, Credentials.Select(c => c.ToXml()));
}

/// <summary>The <c>Sender</c> header party: credentials plus the sending software's <c>UserAgent</c>.</summary>
public sealed class SenderParty : Party
{
    /// <summary>Name of the sending software.</summary>
    public string UserAgent { get; set; } = string.Empty;

    internal static SenderParty ParseSender(XElement e)
    {
        var sender = Parse<SenderParty>(e);
        sender.UserAgent = e.Element("UserAgent")?.Value.Trim() ?? string.Empty;
        return sender;
    }

    internal XElement ToSenderXml()
    {
        var element = ToXml("Sender");
        element.Add(new XElement("UserAgent", UserAgent));
        return element;
    }
}
