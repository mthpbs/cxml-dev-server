using System.Text;
using Cxml.Protocol;
using Cxml.Protocol.Writing;
using CxmlDevServer.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Core.Pipeline;

/// <summary>
/// Runs an inbound cXML document through the registered <see cref="ICxmlPipelineStep"/>s and returns the response
/// to send. HTTP status policy: every cXML-level outcome (including 401, 406 and 500) is answered with HTTP 200 and
/// the cXML <c>Status</c> in the body, as the cXML specification describes; only a response scenario can change
/// the HTTP status or drop the connection.
/// </summary>
public sealed class CxmlInboundProcessor
{
    private static readonly UTF8Encoding Utf8 = new(false);

    private readonly IReadOnlyList<ICxmlPipelineStep> _steps;
    private readonly CxmlServerOptions _options;
    private readonly CxmlWriter _writer;
    private readonly IServiceProvider _services;
    private readonly ILogger<CxmlInboundProcessor> _logger;

    /// <summary>Creates the processor.</summary>
    public CxmlInboundProcessor(
        IEnumerable<ICxmlPipelineStep> steps,
        IOptions<CxmlServerOptions> options,
        CxmlWriter writer,
        IServiceProvider services,
        ILogger<CxmlInboundProcessor> logger)
    {
        _steps = steps.OrderBy(s => s.Order).ToList();
        _options = options.Value;
        _writer = writer;
        _services = services;
        _logger = logger;
    }

    /// <summary>Processes one inbound document.</summary>
    public async Task<CxmlInboundResult> ProcessAsync(CxmlInboundRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = new CxmlInboundContext(request, _options, _services, cancellationToken);
        try
        {
            await RunAsync(context, 0);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Only reachable when the finalize step itself fails (for example the database is unavailable).
            _logger.LogError(ex, "Inbound pipeline failed outside of the finalize step.");
            context.Response = CxmlResponses.Status(CxmlStatusCodes.InternalServerError, detail: ex.Message);
            context.ResponseText = _writer.Write(context.Response);
            context.ResponseBodyOverride = null;
            context.Abort = false;
            context.HttpStatus = 200;
            context.LogId = null;
        }

        var text = context.Abort ? string.Empty : context.ResponseText ?? string.Empty;
        return new CxmlInboundResult(
            Utf8.GetBytes(text),
            text,
            CxmlVersion.ContentType,
            context.HttpStatus,
            context.DelayApplied,
            context.Abort,
            context.LogId,
            context.Response?.Response?.Status.Code,
            context.Response,
            context.Issues);
    }

    private Task RunAsync(CxmlInboundContext context, int index) =>
        index >= _steps.Count
            ? Task.CompletedTask
            : _steps[index].InvokeAsync(context, () => RunAsync(context, index + 1));
}
