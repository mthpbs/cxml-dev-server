using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>The header total of an OrderRequest should equal the sum of quantity times unit price (plus tax and shipping).</summary>
public sealed class OrderTotalRule : DocumentRuleBase
{
    private const decimal Tolerance = 0.005m;

    /// <inheritdoc />
    public override string Code => "RULE-ORDER-TOTAL";

    /// <inheritdoc />
    public override string Description => "OrderRequestHeader/Total should equal the sum of ItemOut quantity x UnitPrice (optionally plus Tax and Shipping).";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) =>
        PayloadOf<OrderRequest>(context) is { Header.Type: not OrderRequestType.Delete };

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var order = PayloadOf<OrderRequest>(context)!;
        var lines = order.Items.Where(i => i.ItemDetail is not null).Sum(i => i.Quantity * i.ItemDetail!.UnitPrice.Amount);
        var tax = order.Header.Tax?.Amount.Amount ?? 0m;
        var shipping = order.Header.Shipping?.Cost.Amount ?? 0m;
        var total = order.Header.Total.Amount;
        var candidates = new[] { lines, lines + tax, lines + shipping, lines + tax + shipping };
        if (!candidates.Any(c => Math.Abs(c - total) <= Tolerance))
        {
            yield return Warning($"The header total {total:0.00##} does not match the sum of the lines {lines:0.00##} (tax {tax:0.00##}, shipping {shipping:0.00##}).");
        }

        var currencies = order.Items.Select(i => i.ItemDetail?.UnitPrice.Currency)
            .Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        if (currencies.Any(c => !string.Equals(c, order.Header.Total.Currency, StringComparison.OrdinalIgnoreCase)))
        {
            yield return Warning($"Line currencies ({string.Join(", ", currencies)}) differ from the header total currency '{order.Header.Total.Currency}'.");
        }
    }
}
