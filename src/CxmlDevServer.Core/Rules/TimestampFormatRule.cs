using System.Text.RegularExpressions;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>The envelope <c>timestamp</c> must have the form <c>yyyy-MM-ddTHH:mm:sszzz</c> (no <c>Z</c> suffix).</summary>
public sealed partial class TimestampFormatRule : DocumentRuleBase
{
    /// <inheritdoc />
    public override string Code => "RULE-TIMESTAMP-FORMAT";

    /// <inheritdoc />
    public override string Description => "The cXML timestamp must be yyyy-MM-ddTHH:mm:sszzz with a numeric offset such as -08:00 (a 'Z' suffix is not allowed).";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) => context.Envelope.TimestampText is not null;

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var text = context.Envelope.TimestampText!.Trim();
        if (!TimestampRegex().IsMatch(text))
        {
            yield return Error($"The timestamp '{text}' does not match yyyy-MM-ddTHH:mm:sszzz (for example 2024-05-01T10:30:00+02:00).");
        }
        else if (!Cxml.Protocol.CxmlTimestamp.TryParse(text, out _))
        {
            yield return Error($"The timestamp '{text}' is not a valid date and time.");
        }
    }

    /// <summary>Matches the cXML timestamp format (optional fractional seconds are tolerated).</summary>
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?[+-]\d{2}:\d{2}$", RegexOptions.CultureInvariant)]
    public static partial Regex TimestampRegex();
}
