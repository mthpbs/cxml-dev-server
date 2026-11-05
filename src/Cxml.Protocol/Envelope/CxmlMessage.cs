using System.Xml.Linq;
using Cxml.Protocol.Documents;

namespace Cxml.Protocol.Envelope;

/// <summary>The envelope <c>Message</c> wrapper (one-way documents such as <c>PunchOutOrderMessage</c>).</summary>
public sealed class CxmlMessage
{
    /// <summary><c>production</c> or <c>test</c>; <c>null</c> means unspecified.</summary>
    public string? DeploymentMode { get; set; }

    /// <summary>The payloadID of the document this message replies to.</summary>
    public string? InReplyTo { get; set; }

    /// <summary>Optional status (e.g. 204 when a PunchOut session ended without changes).</summary>
    public Status? Status { get; set; }

    /// <summary>The message payload.</summary>
    public ICxmlPayload? Payload { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }
}
