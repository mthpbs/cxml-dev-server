namespace CxmlDevServer.Core.Orders;

/// <summary>Builds the correlation ids that link related message log entries.</summary>
public static class CorrelationIds
{
    /// <summary>The id shared by all messages about one order of one partner.</summary>
    public static string ForOrder(int? partnerId, string orderId) => $"order:{partnerId ?? 0}:{orderId.Trim()}";
}
