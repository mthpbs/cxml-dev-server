using Cxml.Protocol.Envelope;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Pipeline;

/// <summary>What the HTTP layer sends back after <see cref="CxmlInboundProcessor.ProcessAsync"/>.</summary>
/// <param name="ResponseBytes">The response body (UTF-8, no byte order mark). Empty when <paramref name="Abort"/> is set.</param>
/// <param name="ResponseText">The response body as text.</param>
/// <param name="ContentType">The <c>Content-Type</c> to send.</param>
/// <param name="HttpStatus">The HTTP status to send. Always 200 for cXML-level errors unless a scenario overrides it.</param>
/// <param name="DelayApplied">Delay a scenario already applied (informational; do not delay again).</param>
/// <param name="Abort">True when the connection must be dropped without a response (timeout simulation).</param>
/// <param name="LogId">The id of the persisted message log entry, if logging succeeded.</param>
/// <param name="CxmlStatusCode">The cXML <c>Status@code</c> of the response, if it is a cXML response.</param>
/// <param name="ResponseEnvelope">The response envelope, if one was produced.</param>
/// <param name="Issues">All validation issues found on the request.</param>
public sealed record CxmlInboundResult(
    byte[] ResponseBytes,
    string ResponseText,
    string ContentType,
    int HttpStatus,
    TimeSpan DelayApplied,
    bool Abort,
    int? LogId,
    int? CxmlStatusCode,
    CxmlEnvelope? ResponseEnvelope,
    IReadOnlyList<ValidationIssue> Issues);
