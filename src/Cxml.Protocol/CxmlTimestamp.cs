using System.Globalization;
using System.Text.RegularExpressions;

namespace Cxml.Protocol;

/// <summary>
/// Formats and parses cXML date-time values (ISO 8601 with a mandatory numeric offset).
/// </summary>
public static partial class CxmlTimestamp
{
    /// <summary>The .NET format string used for cXML timestamps. Never produces a <c>Z</c> suffix.</summary>
    public const string FormatString = "yyyy-MM-ddTHH:mm:sszzz";

    /// <summary>Formats a value as <c>yyyy-MM-ddTHH:mm:sszzz</c>, e.g. <c>2024-05-01T10:00:00+00:00</c>.</summary>
    public static string Format(DateTimeOffset value) => value.ToString(FormatString, CultureInfo.InvariantCulture);

    /// <summary>Formats the current time.</summary>
    public static string Now() => Format(DateTimeOffset.Now);

    /// <summary>
    /// Tolerantly parses a cXML timestamp. Accepts <c>Z</c>, <c>+hh:mm</c> and <c>+hhmm</c> offsets,
    /// fractional seconds, embedded line breaks, and values without an offset (treated as UTC).
    /// </summary>
    public static bool TryParse(string? text, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (TryParseCore(trimmed, out value))
        {
            return true;
        }

        // Some documents wrap long values across lines; strip all whitespace and retry.
        var compact = WhitespaceRegex().Replace(trimmed, string.Empty);
        return compact != trimmed && TryParseCore(compact, out value);
    }

    /// <summary>Parses a timestamp or returns <c>null</c> when it cannot be parsed.</summary>
    public static DateTimeOffset? ParseOrNull(string? text) => TryParse(text, out var value) ? value : null;

    private static bool TryParseCore(string text, out DateTimeOffset value)
    {
        var normalized = OffsetWithoutColonRegex().Replace(text, "$1:$2");
        return DateTimeOffset.TryParse(
            normalized,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces,
            out value);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(?<=T[\d:.]+)([+-]\d{2})(\d{2})$")]
    private static partial Regex OffsetWithoutColonRegex();
}
