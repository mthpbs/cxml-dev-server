using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Web.Security;

/// <summary>Registers the optional admin login: cookie authentication, the <c>Admin</c> policy and the login rate limiter.</summary>
public static class AdminAuthExtensions
{
    /// <summary>The name of the authorization policy that guards the admin pages.</summary>
    public const string PolicyName = "Admin";

    /// <summary>The name of the rate limiting policy of <c>POST /account/login</c>.</summary>
    public const string LoginRateLimitPolicy = "admin-login";

    /// <summary>The path of the (static) login page.</summary>
    public const string LoginPath = "/login";

    /// <summary>Login attempts allowed per client address and minute.</summary>
    internal const int LoginAttemptsPerMinute = 5;

    /// <summary>
    /// Registers authentication and authorization. Everything that depends on the settings is configured through
    /// options, so the final configuration (environment, command line, tests) decides when the host starts. The
    /// settings are validated at startup: an enabled login without a password stops the application.
    /// </summary>
    public static IServiceCollection AddCxmlAdminAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminOptions>, AdminOptionsValidator>();
        services.AddSingleton<AdminCredentialValidator>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<AdminOptions>>((cookie, admin) =>
            {
                cookie.Cookie.Name = "cxml-dev-admin";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SameSite = SameSiteMode.Lax;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                cookie.SlidingExpiration = true;
                cookie.ExpireTimeSpan = TimeSpan.FromHours(Math.Max(1, admin.Value.SessionHours));
                cookie.LoginPath = LoginPath;
                cookie.ReturnUrlParameter = "returnUrl";
                cookie.AccessDeniedPath = LoginPath;
                // A relative redirect keeps working when a proxy changes scheme or host.
                cookie.Events.OnRedirectToLogin = redirect =>
                {
                    var request = redirect.Request;
                    var target = Uri.EscapeDataString($"{request.PathBase}{request.Path}{request.QueryString}");
                    redirect.Response.Redirect($"{request.PathBase}{LoginPath}?returnUrl={target}");
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();
        services.AddOptions<AuthorizationOptions>().Configure<IOptions<AdminOptions>>((authorization, admin) =>
        {
            authorization.AddPolicy(PolicyName, policy =>
            {
                if (admin.Value.Enabled)
                {
                    policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser();
                }
                else
                {
                    // Local runs: the policy exists (the pages reference it) but lets everybody in.
                    policy.RequireAssertion(_ => true);
                }
            });
        });
        services.AddCascadingAuthenticationState();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(LoginRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? IPAddress.None.ToString(),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LoginAttemptsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        });
        return services;
    }

    private sealed class AdminOptionsValidator : IValidateOptions<AdminOptions>
    {
        public ValidateOptionsResult Validate(string? name, AdminOptions options) =>
            options.Validate() is { } problem ? ValidateOptionsResult.Fail(problem) : ValidateOptionsResult.Success;
    }
}
