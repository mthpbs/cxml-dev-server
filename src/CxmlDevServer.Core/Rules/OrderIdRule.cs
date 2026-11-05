using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>An OrderRequest needs an orderID; update and delete requests should carry an orderVersion.</summary>
public sealed class OrderIdRule : DocumentRuleBase
{
    /// <inheritdoc />
    public override string Code => "RULE-ORDERID-PRESENT";

    /// <inheritdoc />
    public override string Description => "OrderRequestHeader@orderID must not be empty; update/delete requests should set orderVersion.";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) => PayloadOf<OrderRequest>(context) is not null;

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var header = PayloadOf<OrderRequest>(context)!.Header;
        if (string.IsNullOrWhiteSpace(header.OrderId))
        {
            yield return Error("OrderRequestHeader@orderID is missing or empty.");
        }

        if (header.Type != OrderRequestType.New && header.OrderVersion is null)
        {
            yield return Warning($"A '{header.Type.ToCxml()}' OrderRequest should carry orderVersion.");
        }
    }
}
