using Cxml.Protocol.Envelope;

namespace CxmlDevServer.Core.Messaging;

/// <summary>The outcome of posting a cXML document to a remote server.</summary>
/// <param name="Succeeded">True when a response was received (whatever its status).</param>
/// <param name="HttpStatus">The HTTP status of the response; <c>null</c> when none was received.</param>
/// <param name="ResponseText">The response body.</param>
/// <param name="ResponseEnvelope">The response parsed as cXML, if it was cXML.</param>
/// <param name="CxmlStatusCode">The cXML <c>Status@code</c> of the response, if it was a cXML Response.</param>
/// <param name="CxmlStatusText">The cXML <c>Status@text</c> of the response.</param>
/// <param name="Duration">The round-trip time.</param>
/// <param name="LogId">The id of the message log entry that holds the request and the response.</param>
/// <param name="Error">What went wrong when no response was received (connection error, timeout, invalid URL).</param>
public sealed record CxmlOutboundResult(
    bool Succeeded,
    int? HttpStatus,
    string? ResponseText,
    CxmlEnvelope? ResponseEnvelope,
    int? CxmlStatusCode,
    string? CxmlStatusText,
    TimeSpan Duration,
    int? LogId,
    string? Error);

/// <summary>
/// Posts cXML documents to remote servers and logs the exchange (one message log entry holding request and
/// response, channel <c>ManualSend</c>). Used by the manual sender; future outbound transactions
/// (ConfirmationRequest, ShipNoticeRequest, ...) build on it.
/// </summary>
public interface ICxmlOutboundClient
{
    /// <summary>Posts <paramref name="xml"/> to <paramref name="url"/> and returns the logged result.</summary>
    Task<CxmlOutboundResult> SendAsync(string url, string xml, CancellationToken cancellationToken = default);
}
