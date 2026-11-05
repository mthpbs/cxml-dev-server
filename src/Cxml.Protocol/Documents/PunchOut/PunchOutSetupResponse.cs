using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Documents.PunchOut;

/// <summary>The supplier's answer to a setup request: the URL where the shopping session starts.</summary>
public sealed class PunchOutSetupResponse : ICxmlPayload
{
    /// <summary>The element name, <c>PunchOutSetupResponse</c>.</summary>
    public const string Name = "PunchOutSetupResponse";

    internal static readonly CxmlPayloadCodec<PunchOutSetupResponse> Codec = new(Name, Parse, p => p.ToXml());

    /// <inheritdoc />
    public string ElementName => Name;

    /// <summary>The <c>StartPage/URL</c> the buyer's browser is sent to.</summary>
    public string StartPageUrl { get; set; } = string.Empty;

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    private static PunchOutSetupResponse Parse(XElement e) => new()
    {
        StartPageUrl = e.Element("StartPage")?.ChildText("URL") ?? string.Empty,
        Raw = e,
    };

    private XElement ToXml() => new(Name, new XElement("StartPage", new XElement("URL", StartPageUrl)));
}
