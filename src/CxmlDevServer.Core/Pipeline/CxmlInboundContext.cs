using System.Diagnostics;
using Cxml.Protocol;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Options;

namespace CxmlDevServer.Core.Pipeline;

/// <summary>State shared by the steps while one inbound document is processed.</summary>
public sealed class CxmlInboundContext
{
    private readonly List<ValidationIssue> _issues = [];

    /// <summary>Creates a context.</summary>
    public CxmlInboundContext(
        CxmlInboundRequest request,
        CxmlServerOptions options,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        Request = request;
        Options = options;
        Services = services;
        CancellationToken = cancellationToken;
        RawBytes = request.Body;
        ContentType = request.ContentType;
        RemoteEndpoint = request.RemoteEndpoint;
        BaseUrl = options.ResolveBaseUrl(request.BaseUrl);
    }

    /// <summary>The original request.</summary>
    public CxmlInboundRequest Request { get; }

    /// <summary>Server options.</summary>
    public CxmlServerOptions Options { get; }

    /// <summary>The request-scoped service provider (for steps and handlers that need extra services).</summary>
    public IServiceProvider Services { get; }

    /// <summary>Cancellation of the HTTP request.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Measures the processing time.</summary>
    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    /// <summary>The body bytes (the decoded XML bytes after form decoding).</summary>
    public byte[] RawBytes { get; set; }

    /// <summary>The cXML text; set by the capture step.</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>The request content type.</summary>
    public string? ContentType { get; set; }

    /// <summary>Remote endpoint of the caller.</summary>
    public string? RemoteEndpoint { get; set; }

    /// <summary>Base URL (no trailing slash) used to build links that point back at this server.</summary>
    public string BaseUrl { get; set; }

    /// <summary>The parsed document; set by the parse step.</summary>
    public CxmlEnvelope? Envelope { get; set; }

    /// <summary>The payload element name, or <c>Unknown</c> before parsing.</summary>
    public string DocumentType { get; set; } = "Unknown";

    /// <summary>The authenticated partner; <c>null</c> before authentication or for allowed unknown senders.</summary>
    public TradingPartner? Partner { get; set; }

    /// <summary>The validation mode in force: the partner's, or the server default when there is no partner.</summary>
    public ValidationMode EffectiveValidationMode => Partner?.ValidationMode ?? Options.DefaultValidationMode;

    /// <summary>All issues recorded so far.</summary>
    public IReadOnlyList<ValidationIssue> Issues => _issues;

    /// <summary>The issues as a validation result.</summary>
    public ValidationResult Validation => new(_issues);

    /// <summary>The response envelope; setting it ends the pipeline (steps must not call <c>next</c>).</summary>
    public CxmlEnvelope? Response { get; set; }

    /// <summary>When set, sent instead of the serialized <see cref="Response"/> (malformed-body scenarios).</summary>
    public string? ResponseBodyOverride { get; set; }

    /// <summary>The serialized response body; set by the finalize step.</summary>
    public string? ResponseText { get; set; }

    /// <summary>The id of the persisted log entry; set by the finalize step.</summary>
    public int? LogId { get; set; }

    /// <summary>The HTTP status to return; 200 unless a scenario says otherwise.</summary>
    public int HttpStatus { get; set; } = 200;

    /// <summary>True to drop the connection without answering.</summary>
    public bool Abort { get; set; }

    /// <summary>Delay already applied by a scenario.</summary>
    public TimeSpan DelayApplied { get; set; }

    /// <summary>The scenario that fired.</summary>
    public int? ScenarioId { get; set; }

    /// <summary>Free-form notes stored in the log.</summary>
    public string? Notes { get; set; }

    /// <summary>The unexpected exception, when the pipeline failed with a 500.</summary>
    public Exception? UnhandledException { get; set; }

    /// <summary>The log entry; created by the capture step and saved by the finalize step.</summary>
    public CxmlMessageLog LogEntry { get; set; } = new();

    /// <summary>Extension point for custom steps and handlers.</summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <summary>True when the pipeline should stop: a response was produced or the connection is to be dropped.</summary>
    public bool IsCompleted => Response is not null || Abort || ResponseBodyOverride is not null;

    /// <summary>Records an issue.</summary>
    public void AddIssue(ValidationIssue issue) => _issues.Add(issue);

    /// <summary>Records several issues.</summary>
    public void AddIssues(IEnumerable<ValidationIssue> issues) => _issues.AddRange(issues);

    /// <summary>Removes recorded issues matching <paramref name="predicate"/>.</summary>
    public void RemoveIssues(Predicate<ValidationIssue> predicate) => _issues.RemoveAll(predicate);

    /// <summary>Ends the pipeline with a cXML status response.</summary>
    public void Fail(int code, string? detail = null, string? text = null) =>
        Response = CxmlResponses.Status(code, text, detail);
}
