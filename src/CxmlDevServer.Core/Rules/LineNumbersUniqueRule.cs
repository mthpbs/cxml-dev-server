using Cxml.Protocol.Common;
using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>Line numbers of ItemOut elements must be unique within a document.</summary>
public sealed class LineNumbersUniqueRule : DocumentRuleBase
{
    /// <inheritdoc />
    public override string Code => "RULE-LINENUMBERS-UNIQUE";

    /// <inheritdoc />
    public override string Description => "ItemOut@lineNumber values must be unique within an OrderRequest or PunchOutSetupRequest.";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) => Items(context) is not null;

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var duplicates = Items(context)!
            .Where(i => i.LineNumber is not null)
            .GroupBy(i => i.LineNumber!.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Order();
        foreach (var number in duplicates)
        {
            yield return Error($"lineNumber {number} is used by more than one ItemOut.");
        }
    }

    private static List<ItemOut>? Items(CxmlRuleContext context) => context.Envelope.Payload switch
    {
        OrderRequest order => order.Items,
        PunchOutSetupRequest setup => setup.Items,
        _ => null,
    };
}
