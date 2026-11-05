using Microsoft.AspNetCore.Http.HttpResults;
using CxmlDevServer.Core.Messaging;
using CxmlDevServer.Web.Components.TestBuyer;

namespace CxmlDevServer.Web.Endpoints;

/// <summary>
/// The built-in "buyer" side of PunchOut. The browser posts the PunchOutOrderMessage here (the default
/// BrowserFormPost URL of the sender templates), so a whole PunchOut round trip can be tried without a procurement system.
/// </summary>
public static class TestBuyerEndpoints
{
    /// <summary>Maps <c>/test-buyer/punchout-return</c>.</summary>
    public static IEndpointRouteBuilder MapTestBuyerEndpoints(this IEndpointRouteBuilder app)
    {
        // Cross-origin by design: the form is posted by the supplier's page, so there is no antiforgery token.
        app.MapPost("/test-buyer/punchout-return", async (HttpContext context, PunchOutReturnReceiver receiver) =>
            {
                if (!context.Request.HasFormContentType)
                {
                    return new RazorComponentResult<PunchOutReturnPage>(new
                    {
                        Result = new PunchOutReturnResult { Error = "The request is not a form post (content type " + (context.Request.ContentType ?? "none") + ")." },
                    });
                }

                var form = await context.Request.ReadFormAsync(context.RequestAborted);
                var fields = form.SelectMany(f => f.Value.Select(v => new KeyValuePair<string, string?>(f.Key, v))).ToList();
                var remote = context.Connection.RemoteIpAddress?.ToString();
                var result = await receiver.ReceiveAsync(fields, remote, context.RequestAborted);
                return new RazorComponentResult<PunchOutReturnPage>(new { Result = result });
            })
            .DisableAntiforgery()
            .ExcludeFromDescription();

        app.MapGet("/test-buyer/punchout-return", () => Results.Redirect("/"))
            .ExcludeFromDescription();
        return app;
    }
}
