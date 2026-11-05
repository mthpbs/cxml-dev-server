using System.Globalization;

namespace CxmlDevServer.Web.Services;

/// <summary>Display formatting helpers (times are shown in UTC, amounts with the invariant culture).</summary>
public static class Fmt
{
    /// <summary>Date and time in UTC, to the second.</summary>
    public static string Time(DateTimeOffset? value) =>
        value is null ? "-" : value.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>Time in UTC without the date.</summary>
    public static string ClockTime(DateTimeOffset value) => value.UtcDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>An amount with two (or up to four) decimals and its currency.</summary>
    public static string Money(decimal amount, string? currency)
    {
        var text = amount == decimal.Round(amount, 2)
            ? amount.ToString("N2", CultureInfo.InvariantCulture)
            : amount.ToString("#,##0.00##", CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(currency) ? text : $"{text} {currency}";
    }

    /// <summary>A quantity without trailing zeros.</summary>
    public static string Quantity(decimal quantity) => quantity.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>Shortens text to a maximum length with an ellipsis.</summary>
    public static string Truncate(string? text, int max) =>
        string.IsNullOrEmpty(text) || text.Length <= max ? text ?? string.Empty : text[..(max - 1)] + "…";
}
