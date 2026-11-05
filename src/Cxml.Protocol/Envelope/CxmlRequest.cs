using System.Xml.Linq;
using Cxml.Protocol.Documents;

namespace Cxml.Protocol.Envelope;

/// <summary>The envelope <c>Request</c> wrapper holding exactly one request payload.</summary>
public sealed class CxmlRequest
{
    /// <summary><c>production</c> or <c>test</c>; <c>null</c> means unspecified (defaults to production).</summary>
    public string? DeploymentMode { get; set; }

    /// <summary>The request payload (e.g. <c>OrderRequest</c>); an <see cref="UnknownPayload"/> if unrecognised.</summary>
    public ICxmlPayload? Payload { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }
}
