using Cxml.Protocol.Envelope;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>Warns when the request's deploymentMode differs from the one expected for the partner.</summary>
public sealed class DeploymentModeRule : DocumentRuleBase
{
    /// <inheritdoc />
    public override string Code => "RULE-DEPLOYMENT-MODE";

    /// <inheritdoc />
    public override string Description => "Request@deploymentMode (default production) should match the partner's expected deployment mode.";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) =>
        context.Envelope.Kind == CxmlEnvelopeKind.Request && !string.IsNullOrWhiteSpace(context.Partner?.DeploymentMode);

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var actual = context.Envelope.Request!.DeploymentMode ?? Cxml.Protocol.CxmlVersion.DeploymentProduction;
        var expected = context.Partner!.DeploymentMode!.Trim();
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            yield return Warning($"The request has deploymentMode '{actual}' but partner '{context.Partner.Name}' is configured for '{expected}'.");
        }
    }
}
