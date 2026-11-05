using System.Net;
using System.Text;

namespace CxmlDevServer.Web.Services;

/// <summary>
/// Builds the small server-rendered HTML pages that must work without Blazor: the auto-submitting form that returns
/// a PunchOut cart to the buyer, and error pages for the checkout and cancel endpoints.
/// </summary>
internal static class PlainHtml
{
    private static string E(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

    private static StringBuilder Head(string title)
    {
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html>\n<html lang=\"en\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append("<title>").Append(E(title)).Append("</title>")
            .Append("<link rel=\"stylesheet\" href=\"/lib/bootstrap/dist/css/bootstrap.min.css\">")
            .Append("<link rel=\"stylesheet\" href=\"/app.css\">")
            .Append("</head><body class=\"plain-page\"><main class=\"container py-5\" style=\"max-width: 860px\">");
        return html;
    }

    /// <summary>
    /// The page that posts the PunchOutOrderMessage to the buyer. <paramref name="fieldValue"/> is written into the
    /// hidden field verbatim: the cXML-urlencoded value is already HTML-encoded as the specification requires.
    /// </summary>
    public static string ReturnForm(string actionUrl, string fieldName, string fieldValue, string xml, bool cancelled, bool autoSubmit)
    {
        var html = Head("Returning to your procurement system");
        html.Append("<div class=\"card shadow-sm\"><div class=\"card-body p-4\">")
            .Append("<h1 class=\"h4 mb-2\">")
            .Append(cancelled ? "Leaving the PunchOut session" : "Returning your cart")
            .Append("</h1><p class=\"text-body-secondary\">")
            .Append(autoSubmit
                ? "You are being sent back to your procurement system. If nothing happens, press Continue."
                : "Review the message below, then press Continue to send it to your procurement system.")
            .Append("</p>");

        // The form: one hidden field, a visible submit button for browsers without JavaScript.
        html.Append("<form id=\"punchout-return\" method=\"post\" action=\"").Append(E(actionUrl)).Append("\">")
            .Append("<input type=\"hidden\" name=\"").Append(E(fieldName)).Append("\" value=\"").Append(fieldValue).Append("\">")
            .Append("<button type=\"submit\" class=\"btn btn-primary btn-lg\">Continue</button>")
            .Append("</form>");

        html.Append("<p class=\"small text-body-secondary mt-3 mb-2\">Posting to <code>").Append(E(actionUrl))
            .Append("</code> as <code>").Append(E(fieldName)).Append("</code>.</p>");
        html.Append("<details").Append(autoSubmit ? string.Empty : " open").Append("><summary class=\"mb-2\">Show the cXML being returned</summary>")
            .Append("<pre class=\"xml-view\">").Append(E(XmlFormatter.Pretty(xml))).Append("</pre></details>");
        html.Append("</div></div>");

        if (autoSubmit)
        {
            html.Append("<script>document.getElementById('punchout-return').submit();</script>");
        }

        return html.Append("</main></body></html>").ToString();
    }

    /// <summary>A page that says the PunchOut session was closed and nothing is sent back (inspect sessions).</summary>
    public static string Closed(string title, string message, string homeUrl)
    {
        var html = Head(title);
        html.Append("<div class=\"card shadow-sm\"><div class=\"card-body p-4\"><h1 class=\"h4\">").Append(E(title)).Append("</h1>")
            .Append("<p>").Append(E(message)).Append("</p>")
            .Append("<a class=\"btn btn-outline-secondary\" href=\"").Append(E(homeUrl)).Append("\">Back to the dashboard</a>")
            .Append("</div></div>");
        return html.Append("</main></body></html>").ToString();
    }

    /// <summary>An error page with an optional link back to the shop.</summary>
    public static string Error(string title, string message, string? backUrl)
    {
        var html = Head(title);
        html.Append("<div class=\"card border-danger shadow-sm\"><div class=\"card-body p-4\"><h1 class=\"h4 text-danger\">")
            .Append(E(title)).Append("</h1><p>").Append(E(message)).Append("</p>");
        if (backUrl is not null)
        {
            html.Append("<a class=\"btn btn-outline-secondary\" href=\"").Append(E(backUrl)).Append("\">Back</a>");
        }

        html.Append("</div></div>");
        return html.Append("</main></body></html>").ToString();
    }
}
