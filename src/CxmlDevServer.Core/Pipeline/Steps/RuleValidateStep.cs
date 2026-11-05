using System.Text;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Rules;
using Microsoft.Extensions.Logging;

namespace CxmlDevServer.Core.Pipeline.Steps;

/// <summary>
/// Runs the registered business rules and enforces the partner's validation mode: in Strict mode any DTD or rule
/// error is answered with 406 and the issue list; Lenient keeps the issues for the log; Off discards them.
/// </summary>
public sealed class RuleValidateStep : ICxmlPipelineStep
{
    private const int MaxIssuesInDetail = 20;

    private readonly IReadOnlyList<IDocumentRule> _rules;
    private readonly ILogger<RuleValidateStep> _logger;

    /// <summary>Creates the step.</summary>
    public RuleValidateStep(IEnumerable<IDocumentRule> rules, ILogger<RuleValidateStep> logger)
    {
        _rules = rules.ToList();
        _logger = logger;
    }

    /// <inheritdoc />
    public int Order => CxmlPipelineOrder.RuleValidate;

    /// <inheritdoc />
    public Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        var mode = context.EffectiveValidationMode;
        if (mode == ValidationMode.Off)
        {
            context.RemoveIssues(i => i.Source is ValidationSource.Dtd or ValidationSource.Rule);
            return next();
        }

        var ruleContext = new CxmlRuleContext(context.Envelope!, context.RawText, context.Partner);
        foreach (var rule in _rules)
        {
            try
            {
                if (rule.AppliesTo(ruleContext))
                {
                    context.AddIssues(rule.Evaluate(ruleContext));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rule {Rule} failed.", rule.Code);
                context.AddIssue(new ValidationIssue(
                    ValidationSeverity.Warning, rule.Code, $"The rule could not be evaluated: {ex.Message}", Source: ValidationSource.Rule));
            }
        }

        var errors = context.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
        if (mode == ValidationMode.Strict && errors.Count > 0)
        {
            context.Fail(406, FormatDetail(errors));
            return Task.CompletedTask;
        }

        return next();
    }

    private static string FormatDetail(IReadOnlyList<ValidationIssue> errors)
    {
        var detail = new StringBuilder("The document failed validation (Strict mode):");
        foreach (var error in errors.Take(MaxIssuesInDetail))
        {
            detail.Append('\n').Append(error);
        }

        if (errors.Count > MaxIssuesInDetail)
        {
            detail.Append("\n... and ").Append(errors.Count - MaxIssuesInDetail).Append(" more.");
        }

        return detail.ToString();
    }
}
