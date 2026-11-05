using System.Xml.Linq;
using Cxml.Protocol.Documents;

namespace Cxml.Protocol.Envelope;

/// <summary>The envelope <c>Response</c>: a <see cref="Status"/> and, for 2xx codes, an optional payload.</summary>
public sealed class CxmlResponse
{
    /// <summary>The result status.</summary>
    public Status Status { get; set; } = new();

    /// <summary>The response payload; only valid alongside a 2xx status.</summary>
    public ICxmlPayload? Payload { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }
}
