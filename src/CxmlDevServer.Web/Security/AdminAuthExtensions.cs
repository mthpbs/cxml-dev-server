using System.Net;
using System.Threading.RateLimiting;
using CxmlDevServer.Core.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Web.Security;

/// <summary>
/// Registers the optional admin login: cookie authentication, the optional Microsoft 365 (OpenID Connect) sign-in
/// that signs into the same cookie, the <c>Admin</c> policy and the login rate limiter.
/// </summary>
public static class AdminAuthExtensions
{
    /// <summary>The name of the authorization policy that guards the admin pages.</summary>
    public const string PolicyName = "Admin";

    /// <summary>The name of the rate limiting policy of <c>POST /account/login</c>.</summary>
    public const string LoginRateLimitPolicy = "admin-login";

    /// <summary>The path of the (static) login page.</summary>
    public const string LoginPath = "/login";

    /// <summary>The name of the OpenID Connect authentication scheme of the Microsoft 365 sign-in.</summary>
    public const string MicrosoftScheme = "Microsoft";

    /// <summary>The path of <c>POST /account/login/microsoft</c>, which starts the Microsoft sign-in.</summary>
    public const string MicrosoftLoginPath = "/account/login/microsoft";

    /// <summary>The claim in the login cookie that tells how the admin signed in.</summary>
    public const string AuthMethodClaim = "cxml_auth";

    /// <summary>Value of <see cref="AuthMethodClaim"/> for the password login.</summary>
    public const string AuthMethodPassword = "password";

    /// <summary>Value of <see cref="AuthMethodClaim"/> for the Microsoft 365 sign-in.</summary>
    public const string AuthMethodMicrosoft = "microsoft";

    /// <summary>Login attempts allowed per client address and minute.</summary>
    internal const int LoginAttemptsPerMinute = 5;

    /// <summary>
    /// Registers authentication and authorization. Everything that depends on the settings is configured through
    /// options, so the final configuration (environment, command line, tests) decides when the host starts. The
    /// settings are validated at startup: an enabled login without any sign-in method stops the application.
    /// </summary>
    public static IServiceCollection AddCxmlAdminAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminOptions>, AdminOptionsValidator>();
        services.AddSingleton<AdminCredentialValidator>();

        // The OpenID Connect scheme is always registered (it is cheap and keeps registration free of configuration
        // reads); AddMicrosoftOpenIdConnectOptions fills it in from the settings. It is only ever challenged by
        // POST /account/login/microsoft, which refuses to run when the Microsoft sign-in is not configured.
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie()
            .AddOpenIdConnect(MicrosoftScheme, _ => { });
        AddMicrosoftOpenIdConnectOptions(services);
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

    /// <summary>
    /// Configures the OpenID Connect handler: authorization code flow with PKCE against the single configured tenant,
    /// signing into the cookie scheme. Only the ID token is used (no Graph calls, no tokens are stored).
    /// </summary>
    private static void AddMicrosoftOpenIdConnectOptions(IServiceCollection services)
    {
        services.AddOptions<OpenIdConnectOptions>(MicrosoftScheme)
            .Configure<IOptions<AdminOptions>, IOptions<CxmlServerOptions>, ILoggerFactory>((oidc, admin, server, loggerFactory) =>
            {
                var microsoft = admin.Value.Microsoft;
                var configured = admin.Value.Enabled && microsoft.IsConfigured;
                var logger = loggerFactory.CreateLogger("CxmlDevServer.Web.Security.MicrosoftSignIn");

                // The handler runs for every request and rejects options without these values. When the Microsoft
                // sign-in is not configured, harmless placeholders keep the host starting; nothing ever challenges the
                // scheme and OnMessageReceived refuses a stray callback.
                var tenantId = configured ? microsoft.TenantId!.Trim() : Guid.Empty.ToString();
                oidc.Authority = configured ? microsoft.Authority : $"{MicrosoftSignInOptions.DefaultInstance}{tenantId}/v2.0";
                oidc.ClientId = configured ? microsoft.ClientId!.Trim() : Guid.Empty.ToString();
                oidc.ClientSecret = configured ? microsoft.ClientSecret : null;
                oidc.CallbackPath = string.IsNullOrWhiteSpace(microsoft.CallbackPath) ? "/signin-oidc" : microsoft.CallbackPath;

                oidc.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                oidc.ResponseType = "code";
                oidc.UsePkce = true;
                oidc.SaveTokens = false;
                oidc.GetClaimsFromUserInfoEndpoint = false;
                oidc.MapInboundClaims = false;
                oidc.Scope.Clear();
                oidc.Scope.Add("openid");
                oidc.Scope.Add("profile");
                oidc.Scope.Add("email");
                // Always show the account picker, so that somebody who signed out can switch accounts.
                oidc.Prompt = "select_account";

                // Only ID tokens issued for this tenant are accepted: the issuer of a tenant-specific authority
                // contains the tenant id. (Signature, audience and lifetime are validated by the handler.)
                oidc.TokenValidationParameters.ValidIssuer = oidc.Authority;
                oidc.TokenValidationParameters.NameClaimType = MicrosoftClaims.DisplayNameClaim;

                oidc.Events.OnMessageReceived = context =>
                {
                    if (!configured)
                    {
                        context.Fail("The Microsoft sign-in is not configured.");
                    }

                    return Task.CompletedTask;
                };
                oidc.Events.OnRedirectToIdentityProvider = context =>
                {
                    // Behind a proxy that rewrites the host, the externally visible base URL decides the redirect URI.
                    // The handler stores the (changed) redirect URI in the state after this event, so the code
                    // redemption sends the same value.
                    if (!string.IsNullOrWhiteSpace(server.Value.PublicBaseUrl))
                    {
                        context.ProtocolMessage.RedirectUri = server.Value.ResolveBaseUrl(null) + oidc.CallbackPath;
                    }

                    return Task.CompletedTask;
                };
                oidc.Events.OnTokenValidated = context =>
                {
                    var principal = context.Principal is null ? null : MicrosoftClaims.CreatePrincipal(context.Principal, tenantId);
                    if (principal is null)
                    {
                        logger.LogWarning("Microsoft sign-in refused: the account is not from the configured tenant.");
                        context.Fail("Account is not from the configured tenant.");
                    }
                    else
                    {
                        context.Principal = principal;
                    }

                    return Task.CompletedTask;
                };
                oidc.Events.OnRemoteFailure = context =>
                {
                    // Only the exception type is logged: messages can quote parts of the protocol message.
                    logger.LogWarning("Microsoft sign-in failed ({Reason}).", context.Failure?.GetType().Name ?? "unknown");
                    RedirectToLoginWithSsoError(context.Request, context.Response);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
                oidc.Events.OnAccessDenied = context =>
                {
                    logger.LogInformation("Microsoft sign-in was cancelled or denied.");
                    RedirectToLoginWithSsoError(context.Request, context.Response);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
    }

    // Remote failures never show details to the browser; the log has what is safe to know.
    private static void RedirectToLoginWithSsoError(HttpRequest request, HttpResponse response) =>
        response.Redirect($"{request.PathBase}{LoginPath}?error=sso");

    private sealed class AdminOptionsValidator : IValidateOptions<AdminOptions>
    {
        public ValidateOptionsResult Validate(string? name, AdminOptions options) =>
            options.Validate() is { } problem ? ValidateOptionsResult.Fail(problem) : ValidateOptionsResult.Success;
    }
}
