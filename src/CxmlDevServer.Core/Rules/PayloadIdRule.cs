using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>A payloadID must be present and should have the form <c>datetime.pid.random@host</c>.</summary>
public sealed class PayloadIdRule : DocumentRuleBase
{
    /// <inheritdoc />
    public override string Code => "RULE-PAYLOADID-PRESENT";

    /// <inheritdoc />
    public override string Description => "The cXML payloadID must be present and should look like datetime.pid.random@host.";

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var id = context.Envelope.PayloadId;
        if (string.IsNullOrWhiteSpace(id))
        {
            yield return Error("The payloadID attribute is missing or empty.");
        }
        else if (!id.Contains('@'))
        {
            yield return Warning($"The payloadID '{id}' does not have the recommended form datetime.pid.random@host.");
        }
    }
}
