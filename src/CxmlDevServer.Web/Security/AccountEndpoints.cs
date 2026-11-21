using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Web.Security;

/// <summary>
/// The form posts of the admin login: <c>POST /account/login</c> (password), <c>POST /account/login/microsoft</c>
/// (starts the Microsoft 365 sign-in) and <c>POST /account/logout</c>. All are plain form posts that carry an
/// antiforgery token; the two logins are rate limited per client address. The Microsoft callback
/// (<c>/signin-oidc</c>) is handled by the OpenID Connect middleware, not by an endpoint.
/// </summary>
public static class AccountEndpoints
{
    /// <summary>Maps the login, Microsoft login and logout endpoints.</summary>
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/account/login", LoginAsync)
            .RequireRateLimiting(AdminAuthExtensions.LoginRateLimitPolicy)
            .ExcludeFromDescription();
        app.MapPost(AdminAuthExtensions.MicrosoftLoginPath, MicrosoftLoginAsync)
            .RequireRateLimiting(AdminAuthExtensions.LoginRateLimitPolicy)
            .ExcludeFromDescription();
        app.MapPost("/account/logout", LogoutAsync)
            .ExcludeFromDescription();
        return app;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context, IAntiforgery antiforgery, IOptions<AdminOptions> options, AdminCredentialValidator validator)
    {
        if (!options.Value.Enabled)
        {
            return Results.LocalRedirect("~/");
        }

        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.Text("The login form has expired. Go back, reload the page and try again.", statusCode: StatusCodes.Status400BadRequest);
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var returnUrl = ReturnUrls.Sanitize(form["returnUrl"].ToString());
        if (!validator.IsValid(form["username"].ToString(), form["password"].ToString()))
        {
            return Results.LocalRedirect("~" + LoginPage.WithError(returnUrl));
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, options.Value.Username),
                new Claim(AdminAuthExtensions.AuthMethodClaim, AdminAuthExtensions.AuthMethodPassword),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });
        // The challenge redirect already contains the path base, so the sanitized URL is used as is.
        return Results.LocalRedirect(returnUrl);
    }

    private static async Task<IResult> MicrosoftLoginAsync(
        HttpContext context, IAntiforgery antiforgery, IOptions<AdminOptions> options)
    {
        if (!options.Value.Enabled)
        {
            return Results.LocalRedirect("~/");
        }

        // Without the Microsoft settings the OpenID Connect scheme must never be challenged.
        if (!options.Value.Microsoft.IsConfigured)
        {
            return Results.LocalRedirect("~" + AdminAuthExtensions.LoginPath);
        }

        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.Text("The login form has expired. Go back, reload the page and try again.", statusCode: StatusCodes.Status400BadRequest);
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var returnUrl = ReturnUrls.Sanitize(form["returnUrl"].ToString());
        // The challenge redirect to the login page already contains the path base, so the sanitized URL is used as is
        // (as in the password login). The login is a session cookie: it ends with the browser session.
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl, IsPersistent = false },
            [AdminAuthExtensions.MicrosoftScheme]);
    }

    private static async Task<IResult> LogoutAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.Text("The form has expired. Go back, reload the page and try again.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Only the local cookie ends. The Microsoft session is left alone (single sign-out is not wanted for a dev
        // tool); the account picker (prompt=select_account) lets the next person choose another account.
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("~" + AdminAuthExtensions.LoginPath);
    }

    private static async Task<bool> IsAntiforgeryValidAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}

/// <summary>Local-only return URLs for the login redirect.</summary>
internal static class ReturnUrls
{
    /// <summary>
    /// Returns <paramref name="returnUrl"/> when it is a path on this site (one leading slash, not <c>//host</c> or
    /// <c>/\host</c>, no control characters, not the login page itself), otherwise <c>/</c>.
    /// </summary>
    public static string Sanitize(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) || returnUrl[0] != '/' || returnUrl.Length > 2048)
        {
            return "/";
        }

        if (returnUrl.Length > 1 && returnUrl[1] is '/' or '\\')
        {
            return "/";
        }

        if (returnUrl.Any(char.IsControl))
        {
            return "/";
        }

        var path = returnUrl.Split('?', '#')[0];
        return path.TrimEnd('/').Equals(AdminAuthExtensions.LoginPath, StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/account/", StringComparison.OrdinalIgnoreCase)
            ? "/"
            : returnUrl;
    }
}

/// <summary>Builds links to the login page.</summary>
internal static class LoginPage
{
    /// <summary>The login URL that comes back with an error message.</summary>
    public static string WithError(string returnUrl) =>
        $"{AdminAuthExtensions.LoginPath}?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}";
}
