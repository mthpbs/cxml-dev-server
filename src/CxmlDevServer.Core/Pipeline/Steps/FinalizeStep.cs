using Cxml.Protocol;
using Cxml.Protocol.Writing;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using Microsoft.Extensions.Logging;

namespace CxmlDevServer.Core.Pipeline.Steps;

/// <summary>
/// The outermost step: runs everything else, turns unexpected exceptions into a cXML 500, serializes the response
/// and persists the message log entry with its validation issues.
/// </summary>
public sealed class FinalizeStep : ICxmlPipelineStep
{
    private readonly ICxmlDbContext _db;
    private readonly CxmlWriter _writer;
    private readonly TimeProvider _time;
    private readonly ILogger<FinalizeStep> _logger;

    /// <summary>Creates the step.</summary>
    public FinalizeStep(ICxmlDbContext db, CxmlWriter writer, TimeProvider time, ILogger<FinalizeStep> logger)
    {
        _db = db;
        _writer = writer;
        _time = time;
        _logger = logger;
    }

    /// <inheritdoc />
    public int Order => CxmlPipelineOrder.Finalize;

    /// <inheritdoc />
    public async Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while processing {DocumentType}.", context.DocumentType);
            context.UnhandledException = ex;
            _db.ChangeTracker.Clear();
            context.ResponseBodyOverride = null;
            context.Abort = false;
            context.Response = CxmlResponses.Status(
                CxmlStatusCodes.InternalServerError,
                detail: $"Unexpected error: {ex.GetType().Name}: {ex.Message}");
        }

        if (!context.IsCompleted)
        {
            context.Response = CxmlResponses.Status(CxmlStatusCodes.InternalServerError, detail: "No step produced a response.");
        }

        context.ResponseText = context.Abort
            ? null
            : context.ResponseBodyOverride ?? _writer.Write(context.Response!);

        await PersistLogAsync(context);
    }

    private async Task PersistLogAsync(CxmlInboundContext context)
    {
        var log = context.LogEntry;
        log.Direction = MessageDirection.Inbound;
        log.Channel = context.Request.Channel;
        log.DocumentType = context.DocumentType;
        log.PartnerId = context.Partner?.Id ?? log.PartnerId;
        log.RawXml = context.RawText;
        log.ResponseXml = context.ResponseText;
        log.HttpStatus = context.Abort ? null : context.HttpStatus;
        log.CxmlStatusCode = context.Response?.Response?.Status.Code;
        log.CxmlStatusText = context.Response?.Response?.Status.Text;
        log.ScenarioId = context.ScenarioId;
        log.Notes = context.UnhandledException is { } ex
            ? $"{context.Notes} {ex}".Trim()
            : context.Notes ?? (context.Abort ? "Connection aborted without a response." : null);
        log.DurationMs = context.Stopwatch.ElapsedMilliseconds;
        foreach (var issue in context.Issues)
        {
            log.Issues.Add(ValidationIssueRecord.From(issue));
        }

        var tracked = _db.ChangeTracker.Entries<CxmlMessageLog>().Any(e => ReferenceEquals(e.Entity, log));
        if (!tracked)
        {
            if (log.Id == 0)
            {
                _db.MessageLogs.Add(log);
            }
            else
            {
                _db.MessageLogs.Update(log);
            }
        }

        await _db.SaveChangesAsync(CancellationToken.None);
        context.LogId = log.Id;
    }
}
