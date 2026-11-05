using System.Xml.Linq;

namespace Cxml.Protocol.Envelope;

/// <summary>The envelope <c>Header</c>: From, To and Sender.</summary>
public sealed class CxmlHeader
{
    /// <summary>Logical source of the document.</summary>
    public Party From { get; set; } = new();

    /// <summary>Logical destination of the document.</summary>
    public Party To { get; set; } = new();

    /// <summary>The party transmitting the document, with authentication.</summary>
    public SenderParty Sender { get; set; } = new();

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static CxmlHeader Parse(XElement e) => new()
    {
        From = e.Element("From") is { } f ? Party.Parse<Party>(f) : new Party(),
        To = e.Element("To") is { } t ? Party.Parse<Party>(t) : new Party(),
        Sender = e.Element("Sender") is { } s ? SenderParty.ParseSender(s) : new SenderParty(),
        Raw = e,
    };

    internal XElement ToXml() => new("Header", From.ToXml("From"), To.ToXml("To"), Sender.ToSenderXml());
}
