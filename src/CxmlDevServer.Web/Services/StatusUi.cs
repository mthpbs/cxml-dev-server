using CxmlDevServer.Core.Domain;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Web.Services;

/// <summary>Maps statuses to Bootstrap colors and labels.</summary>
public static class StatusUi
{
    /// <summary>Bootstrap background class for a cXML or HTTP status code: 2xx green, 4xx orange, 5xx red.</summary>
    public static string CodeClass(int? code) => code switch
    {
        null => "text-bg-secondary",
        >= 200 and < 300 => "text-bg-success",
        >= 400 and < 500 => "text-bg-warning",
        >= 500 => "text-bg-danger",
        _ => "text-bg-secondary",
    };

    /// <summary>Bootstrap class for a validation severity.</summary>
    public static string SeverityClass(ValidationSeverity severity) => severity switch
    {
        ValidationSeverity.Error => "text-bg-danger",
        ValidationSeverity.Warning => "text-bg-warning",
        _ => "text-bg-info",
    };

    /// <summary>The status to show: a session that is still open but past its expiry is shown as expired.</summary>
    public static PunchOutSessionStatus Effective(PunchOutSession session) =>
        session.IsOpenForShopping && session.ExpiresAt <= DateTimeOffset.UtcNow ? PunchOutSessionStatus.Expired : session.Status;

    /// <summary>Bootstrap class for a PunchOut session status.</summary>
    public static string SessionClass(PunchOutSessionStatus status) => status switch
    {
        PunchOutSessionStatus.Created => "text-bg-secondary",
        PunchOutSessionStatus.Opened => "text-bg-primary",
        PunchOutSessionStatus.Submitted => "text-bg-success",
        PunchOutSessionStatus.Cancelled => "text-bg-warning",
        _ => "text-bg-dark",
    };

    /// <summary>Bootstrap class for a purchase order status.</summary>
    public static string OrderClass(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Received => "text-bg-success",
        PurchaseOrderStatus.Updated => "text-bg-primary",
        PurchaseOrderStatus.Cancelled => "text-bg-warning",
        _ => "text-bg-danger",
    };
}
