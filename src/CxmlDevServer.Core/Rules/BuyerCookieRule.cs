using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>The BuyerCookie of a PunchOut setup request must not be empty.</summary>
public sealed class BuyerCookieRule : DocumentRuleBase
{
    /// <inheritdoc />
    public override string Code => "RULE-BUYERCOOKIE-NONEMPTY";

    /// <inheritdoc />
    public override string Description => "PunchOutSetupRequest BuyerCookie must not be empty.";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) => PayloadOf<PunchOutSetupRequest>(context) is not null;

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        if (string.IsNullOrWhiteSpace(PayloadOf<PunchOutSetupRequest>(context)!.BuyerCookie))
        {
            yield return Error("BuyerCookie is empty; it must carry a value that identifies the buyer's session.");
        }
    }
}
