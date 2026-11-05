using Cxml.Protocol;
using Cxml.Protocol.Envelope;
using CxmlDevServer.Core.Handlers;

namespace CxmlDevServer.Core.Pipeline.Steps;

/// <summary>Hands requests to the <see cref="ICxmlRequestHandler"/> registered for the payload element.</summary>
public sealed class DispatchStep : ICxmlPipelineStep
{
    private readonly IReadOnlyDictionary<string, ICxmlRequestHandler> _handlers;

    /// <summary>Creates the step.</summary>
    public DispatchStep(IEnumerable<ICxmlRequestHandler> handlers) =>
        _handlers = handlers
            .GroupBy(h => h.RequestName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);

    /// <inheritdoc />
    public int Order => CxmlPipelineOrder.Dispatch;

    /// <inheritdoc />
    public async Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        var envelope = context.Envelope!;
        var supported = string.Join(", ", _handlers.Keys.Order(StringComparer.Ordinal));
        switch (envelope.Kind)
        {
            case CxmlEnvelopeKind.Response:
                context.Fail(CxmlStatusCodes.BadRequest, "A Response document cannot be posted to this endpoint; send a Request.");
                return;
            case CxmlEnvelopeKind.Message:
                context.Fail(
                    CxmlStatusCodes.NotImplemented,
                    $"Message documents ('{context.DocumentType}') are not accepted by this endpoint. Supported requests: {supported}.");
                return;
            case CxmlEnvelopeKind.None:
                context.Fail(CxmlStatusCodes.BadRequest, "The document contains no Request, Response or Message.");
                return;
        }

        var payload = envelope.Request!.Payload;
        if (payload is null || !_handlers.TryGetValue(payload.ElementName, out var handler))
        {
            var name = payload?.ElementName ?? "(none)";
            context.Fail(CxmlStatusCodes.NotImplemented, $"Request '{name}' is not supported. Supported requests: {supported}.");
            return;
        }

        var result = await handler.HandleAsync(context);
        context.AddIssues(result.Issues);
        if (result.CorrelationId is not null)
        {
            context.LogEntry.CorrelationId = result.CorrelationId;
        }

        context.Response = CxmlResponses.Status(result.StatusCode, result.StatusText, result.Detail, result.Payload);
    }
}
