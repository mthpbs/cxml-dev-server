using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CxmlDevServer.Web.Tests.Support;

internal static partial class Http
{
    public static Task<HttpResponseMessage> PostCxmlAsync(this HttpClient client, string xml) =>
        client.PostAsync("/cxml", new StringContent(xml, Encoding.UTF8, "text/xml"));

    [GeneratedRegex("<input[^>]*name=\"__RequestVerificationToken\"[^>]*>")]
    private static partial Regex AntiforgeryInputRegex();

    [GeneratedRegex("value=\"([^\"]*)\"")]
    private static partial Regex ValueAttributeRegex();

    /// <summary>The antiforgery token rendered into the first form of a page.</summary>
    public static string ExtractAntiforgeryToken(string html)
    {
        var input = AntiforgeryInputRegex().Match(html);
        Assert.True(input.Success, "The page contains no antiforgery token.");
        return WebUtility.HtmlDecode(ValueAttributeRegex().Match(input.Value).Groups[1].Value);
    }

    [GeneratedRegex("""<form[^>]*id="punchout-return"[^>]*action="(?<action>[^"]*)"[^>]*>\s*<input type="hidden" name="(?<name>[^"]*)" value="(?<value>[^"]*)">""")]
    private static partial Regex ReturnFormRegex();

    /// <summary>The action URL, field name and (HTML-decoded) field value of the PunchOut return form.</summary>
    public static (string Action, string Field, string Value) ParseReturnForm(string html)
    {
        var match = ReturnFormRegex().Match(html);
        Assert.True(match.Success, "The page contains no PunchOut return form.");
        return (
            WebUtility.HtmlDecode(match.Groups["action"].Value),
            WebUtility.HtmlDecode(match.Groups["name"].Value),
            WebUtility.HtmlDecode(match.Groups["value"].Value));
    }
}
