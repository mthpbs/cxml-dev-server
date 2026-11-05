using System.Collections.Concurrent;
using CxmlDevServer.Core.PunchOut;
using CxmlDevServer.Web.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace CxmlDevServer.Web.Endpoints;

/// <summary>
/// The endpoints that leave the PunchOut shop: <c>POST /punchout/{token}/checkout</c> and <c>/cancel</c>. Both are
/// plain form posts from the shop page (protected by an antiforgery token) that answer with an HTML page whose form
/// posts the PunchOutOrderMessage to the buyer's BrowserFormPost URL.
/// </summary>
public static class PunchOutEndpoints
{
    // One checkout per session at a time, so a double click cannot return the cart twice.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    /// <summary>Maps the checkout and cancel endpoints.</summary>
    public static IEndpointRouteBuilder MapPunchOutEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/punchout/{token}/checkout", (HttpContext context, string token) => LeaveAsync(context, token, cancel: false))
            .ExcludeFromDescription();
        app.MapPost("/punchout/{token}/cancel", (HttpContext context, string token) => LeaveAsync(context, token, cancel: true))
            .ExcludeFromDescription();
        return app;
    }

    private static async Task<IResult> LeaveAsync(HttpContext context, string token, bool cancel)
    {
        var shopUrl = $"{context.Request.PathBase}/punchout/{Uri.EscapeDataString(token)}";
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Html(PlainHtml.Error(
                "This form has expired",
                "The security token of the page was missing or no longer valid. Go back to the shop, reload the page and try again.",
                shopUrl), StatusCodes.Status400BadRequest);
        }

        var review = context.Request.Form.ContainsKey("review");
        var gate = Locks.GetOrAdd(token, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(context.RequestAborted);
        try
        {
            var ui = context.RequestServices.GetRequiredService<PunchOutService>();
            var result = cancel
                ? await ui.CancelAsync(token, context.RequestAborted)
                : await ui.CheckoutAsync(token, context.RequestAborted);
            if (result is null)
            {
                return Html(PlainHtml.Closed(
                    "Session closed",
                    "This inspect session has no return address, so nothing was sent back. You can close this window.",
                    $"{context.Request.PathBase}/"));
            }

            return Html(PlainHtml.ReturnForm(
                result.FormActionUrl, result.FieldName, result.FieldValue, result.Xml, result.Cancelled, autoSubmit: !review));
        }
        catch (PunchOutSessionException ex)
        {
            var status = ex.Error switch
            {
                PunchOutError.NotFound => StatusCodes.Status404NotFound,
                PunchOutError.Invalid => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status409Conflict,
            };
            return Html(PlainHtml.Error("Cannot leave the PunchOut session", ex.Message, ex.Error == PunchOutError.NotFound ? null : shopUrl), status);
        }
        finally
        {
            gate.Release();
        }
    }

    private static IResult Html(string html, int status = StatusCodes.Status200OK) =>
        Results.Content(html, "text/html; charset=utf-8", statusCode: status);
}
