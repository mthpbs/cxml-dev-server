using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>A BrowserFormPost URL, when given, must be an absolute http or https URL.</summary>
public sealed class BrowserFormPostUrlRule : DocumentRuleBase
{
    /// <inheritdoc />
    public override string Code => "RULE-BROWSERFORMPOST-URL";

    /// <inheritdoc />
    public override string Description => "PunchOutSetupRequest BrowserFormPost/URL must be an absolute http(s) URL.";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) => PayloadOf<PunchOutSetupRequest>(context) is not null;

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var url = PayloadOf<PunchOutSetupRequest>(context)!.BrowserFormPostUrl;
        if (!string.IsNullOrWhiteSpace(url)
            && !(Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
        {
            yield return Error($"BrowserFormPost URL '{url}' is not an absolute http or https URL.");
        }
    }
}
