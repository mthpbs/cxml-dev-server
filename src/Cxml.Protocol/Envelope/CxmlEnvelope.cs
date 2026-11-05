using System.Xml.Linq;
using Cxml.Protocol.Documents;

namespace Cxml.Protocol.Envelope;

/// <summary>Which of the three top-level shapes an envelope has.</summary>
public enum CxmlEnvelopeKind
{
    /// <summary>No Request, Response or Message was present.</summary>
    None,
    /// <summary>Header plus Request.</summary>
    Request,
    /// <summary>Header plus Message.</summary>
    Message,
    /// <summary>A bare Response.</summary>
    Response,
}

/// <summary>
/// The root <c>cXML</c> element: <c>((Header, (Message | Request)) | Response)</c>.
/// </summary>
public sealed class CxmlEnvelope
{
    /// <summary>Unique document id, <c>payloadID</c> attribute.</summary>
    public string PayloadId { get; set; } = string.Empty;

    /// <summary>The <c>timestamp</c> attribute (default value if it could not be parsed).</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>The <c>timestamp</c> attribute exactly as received; <c>null</c> for locally built envelopes.</summary>
    public string? TimestampText { get; set; }

    /// <summary>The <c>xml:lang</c> attribute.</summary>
    public string? Lang { get; set; }

    /// <summary>The optional <c>version</c> attribute.</summary>
    public string? Version { get; set; }

    /// <summary>Header; present for Request and Message envelopes.</summary>
    public CxmlHeader? Header { get; set; }

    /// <summary>Request wrapper, when this is a request envelope.</summary>
    public CxmlRequest? Request { get; set; }

    /// <summary>Response wrapper, when this is a response envelope.</summary>
    public CxmlResponse? Response { get; set; }

    /// <summary>Message wrapper, when this is a message envelope.</summary>
    public CxmlMessage? Message { get; set; }

    /// <summary>The source root element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    /// <summary>The shape of this envelope.</summary>
    public CxmlEnvelopeKind Kind =>
        Request is not null ? CxmlEnvelopeKind.Request :
        Message is not null ? CxmlEnvelopeKind.Message :
        Response is not null ? CxmlEnvelopeKind.Response :
        CxmlEnvelopeKind.None;

    /// <summary>The payload of whichever wrapper is present, if any.</summary>
    public ICxmlPayload? Payload => Request?.Payload ?? Message?.Payload ?? Response?.Payload;
}
