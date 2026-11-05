using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Domain;

/// <summary>A cXML message received or sent by the server, with its response and validation issues.</summary>
public class CxmlMessageLog
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Received or sent.</summary>
    public MessageDirection Direction { get; set; }

    /// <summary>Transport used.</summary>
    public MessageChannel Channel { get; set; }

    /// <summary>Payload element name (<c>OrderRequest</c>, <c>PunchOutSetupRequest</c>, ...), or <c>Unknown</c>.</summary>
    public string DocumentType { get; set; } = "Unknown";

    /// <summary>The payloadID of the message.</summary>
    public string? PayloadId { get; set; }

    /// <summary>The payloadID this message replies to.</summary>
    public string? InReplyTo { get; set; }

    /// <summary>Groups related messages (a PunchOut session, an order).</summary>
    public string? CorrelationId { get; set; }

    /// <summary>The partner the message belongs to.</summary>
    public int? PartnerId { get; set; }

    /// <summary>Navigation to <see cref="PartnerId"/>.</summary>
    public TradingPartner? Partner { get; set; }

    /// <summary>Remote IP endpoint (inbound) or target URL (outbound).</summary>
    public string? RemoteEndpoint { get; set; }

    /// <summary>HTTP status returned (inbound) or received (outbound).</summary>
    public int? HttpStatus { get; set; }

    /// <summary>The cXML <c>Status@code</c> of the response.</summary>
    public int? CxmlStatusCode { get; set; }

    /// <summary>The cXML <c>Status@text</c> of the response.</summary>
    public string? CxmlStatusText { get; set; }

    /// <summary>The message exactly as received or sent.</summary>
    public string RawXml { get; set; } = string.Empty;

    /// <summary>The response body.</summary>
    public string? ResponseXml { get; set; }

    /// <summary>Content type of the inbound request or the outbound response.</summary>
    public string? ContentType { get; set; }

    /// <summary>Processing or round-trip time in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>The response scenario that produced the response, if any.</summary>
    public int? ScenarioId { get; set; }

    /// <summary>Free-form note (scenario name, error text, ...).</summary>
    public string? Notes { get; set; }

    /// <summary>When the message was received or sent.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Validation issues found.</summary>
    public List<ValidationIssueRecord> Issues { get; set; } = [];

    /// <summary>Highest severity among <see cref="Issues"/>, or <c>null</c> when there are none.</summary>
    public ValidationSeverity? WorstSeverity => Issues.Count == 0 ? null : Issues.Max(i => i.Severity);
}
