using Cxml.Protocol;
using Cxml.Protocol.Documents;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Handlers;

/// <summary>The outcome of an <see cref="ICxmlRequestHandler"/>; becomes the cXML <c>Response</c>.</summary>
public sealed class CxmlHandlerResult
{
    /// <summary>The cXML status code.</summary>
    public int StatusCode { get; init; } = CxmlStatusCodes.Ok;

    /// <summary>The status text; defaults to the standard text of the code.</summary>
    public string? StatusText { get; init; }

    /// <summary>Detail for the body of the <c>Status</c> element.</summary>
    public string? Detail { get; init; }

    /// <summary>Response payload (only attached for 2xx codes).</summary>
    public ICxmlPayload? Payload { get; init; }

    /// <summary>Correlation id to store on the message log.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Warnings or infos found while handling the request.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];

    /// <summary>A <c>200 OK</c> result.</summary>
    public static CxmlHandlerResult Ok(
        ICxmlPayload? payload = null,
        string? detail = null,
        string? correlationId = null,
        IReadOnlyList<ValidationIssue>? issues = null) => new()
    {
        StatusCode = CxmlStatusCodes.Ok,
        Payload = payload,
        Detail = detail,
        CorrelationId = correlationId,
        Issues = issues ?? [],
    };

    /// <summary>An error result with the given status code and detail.</summary>
    public static CxmlHandlerResult Fail(
        int statusCode,
        string detail,
        string? correlationId = null,
        IReadOnlyList<ValidationIssue>? issues = null) => new()
    {
        StatusCode = statusCode,
        Detail = detail,
        CorrelationId = correlationId,
        Issues = issues ?? [],
    };
}
