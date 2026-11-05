using Cxml.Protocol;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Pipeline.Steps;

/// <summary>
/// Applies the highest-priority enabled <see cref="ResponseScenario"/> that matches the request: delays, answers
/// with a configured status, a malformed body, or no answer at all. Limited-use scenarios are claimed atomically.
/// </summary>
public sealed class ScenarioStep : ICxmlPipelineStep
{
    private const string TruncatedCxml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><cXML><Response><Status code=\"200\" text=\"OK\"";

    private readonly ICxmlDbContext _db;
    private readonly TimeProvider _time;

    /// <summary>Creates the step.</summary>
    public ScenarioStep(ICxmlDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    /// <inheritdoc />
    public int Order => CxmlPipelineOrder.Scenario;

    /// <inheritdoc />
    public async Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        var scenario = await ClaimMatchingScenarioAsync(context);
        if (scenario is null)
        {
            await next();
            return;
        }

        context.ScenarioId = scenario.Id;
        context.Notes = $"Response scenario '{scenario.Name}' applied ({scenario.Behavior}).";

        if (scenario.DelayMs > 0)
        {
            var delay = TimeSpan.FromMilliseconds(scenario.DelayMs);
            await Task.Delay(delay, _time, context.CancellationToken);
            context.DelayApplied = delay;
        }

        switch (scenario.Behavior)
        {
            case ScenarioBehavior.Passthrough:
                await next();
                return;

            case ScenarioBehavior.NoResponse:
                context.Abort = true;
                return;

            case ScenarioBehavior.MalformedBody:
                context.ResponseBodyOverride = string.IsNullOrEmpty(scenario.MalformedBody) ? TruncatedCxml : scenario.MalformedBody;
                break;

            default:
                var code = scenario.StatusCode ?? CxmlStatusCodes.InternalServerError;
                context.Response = CxmlResponses.Status(
                    code,
                    string.IsNullOrWhiteSpace(scenario.StatusText) ? null : scenario.StatusText,
                    scenario.StatusDetail ?? (CxmlStatusCodes.IsSuccess(code) ? null : $"Simulated by response scenario '{scenario.Name}'."));
                break;
        }

        if (scenario.HttpStatus is { } http)
        {
            context.HttpStatus = http;
        }
    }

    private async Task<ResponseScenario?> ClaimMatchingScenarioAsync(CxmlInboundContext context)
    {
        var partnerId = context.Partner?.Id;
        var documentType = context.DocumentType;
        var candidates = (await _db.ResponseScenarios.AsNoTracking()
                .Where(s => s.Enabled && (s.RemainingUses == null || s.RemainingUses > 0))
                .ToListAsync(context.CancellationToken))
            .Where(s => s.PartnerId == null || s.PartnerId == partnerId)
            .Where(s => string.IsNullOrWhiteSpace(s.DocumentType)
                        || string.Equals(s.DocumentType.Trim(), documentType, StringComparison.OrdinalIgnoreCase))
            .Where(s => string.IsNullOrEmpty(s.MatchContains)
                        || context.RawText.Contains(s.MatchContains, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.Id);

        var now = _time.GetUtcNow();
        foreach (var candidate in candidates)
        {
            var id = candidate.Id;
            var claimed = candidate.RemainingUses is null
                ? await _db.ResponseScenarios.Where(s => s.Id == id)
                    .ExecuteUpdateAsync(
                        u => u.SetProperty(s => s.HitCount, s => s.HitCount + 1).SetProperty(s => s.LastHitAt, now),
                        context.CancellationToken)
                : await _db.ResponseScenarios.Where(s => s.Id == id && s.RemainingUses > 0)
                    .ExecuteUpdateAsync(
                        u => u.SetProperty(s => s.RemainingUses, s => s.RemainingUses - 1)
                              .SetProperty(s => s.HitCount, s => s.HitCount + 1)
                              .SetProperty(s => s.LastHitAt, now),
                        context.CancellationToken);
            if (claimed > 0)
            {
                return candidate;
            }
        }

        return null;
    }
}
