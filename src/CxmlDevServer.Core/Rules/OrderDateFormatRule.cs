using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>The <c>orderDate</c> of an OrderRequest must use the cXML timestamp format.</summary>
public sealed class OrderDateFormatRule : DocumentRuleBase
{
    /// <inheritdoc />
    public override string Code => "RULE-ORDERDATE-FORMAT";

    /// <inheritdoc />
    public override string Description => "OrderRequestHeader@orderDate must be a cXML timestamp (yyyy-MM-ddTHH:mm:sszzz).";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) => PayloadOf<OrderRequest>(context) is not null;

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var text = PayloadOf<OrderRequest>(context)!.Header.OrderDateText?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            yield return Warning("OrderRequestHeader has no orderDate.");
        }
        else if (!TimestampFormatRule.TimestampRegex().IsMatch(text) || !Cxml.Protocol.CxmlTimestamp.TryParse(text, out _))
        {
            yield return Error($"The orderDate '{text}' does not match yyyy-MM-ddTHH:mm:sszzz.");
        }
    }
}
