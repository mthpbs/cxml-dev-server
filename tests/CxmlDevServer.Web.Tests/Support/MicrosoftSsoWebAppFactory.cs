using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CxmlDevServer.Web.Security;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CxmlDevServer.Web.Tests.Support;

/// <summary>
/// The application with the admin login switched on and only the Microsoft 365 sign-in configured (fake tenant and
/// client ids). The OpenID Connect handler gets a static configuration with a test signing key and a fake token
/// endpoint, so no test ever talks to the network.
/// </summary>
public class MicrosoftSsoWebAppFactory : WebAppFactory
{
    public const string TenantId = "11111111-1111-1111-1111-111111111111";
    public const string OtherTenantId = "22222222-2222-2222-2222-222222222222";
    public const string ClientId = "33333333-3333-3333-3333-333333333333";
    public const string ClientSecret = "test-secret";
    public const string AuthorizeEndpoint = "https://login.test/authorize";
    public const string TokenEndpoint = "https://login.test/token";

    private static int _clientCounter;

    /// <summary>The issuer of the ID tokens of <see cref="TenantId"/> (the tenant-specific v2.0 authority).</summary>
    public static string Issuer { get; } = $"https://login.microsoftonline.com/{TenantId}/v2.0";

    /// <summary>The fake identity provider: signs ID tokens and answers the token endpoint.</summary>
    public FakeIdentityProvider IdentityProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Admin:Enabled", "true");
        builder.UseSetting("Admin:Microsoft:TenantId", TenantId);
        builder.UseSetting("Admin:Microsoft:ClientId", ClientId);
        builder.UseSetting("Admin:Microsoft:ClientSecret", ClientSecret);
        // Forwarded headers let every test client pose as its own address (the login rate limit is per address).
        builder.UseSetting("FORWARDEDHEADERS_ENABLED", "true");
        builder.ConfigureTestServices(services =>
        {
            var provider = IdentityProvider;
            services.PostConfigure<OpenIdConnectOptions>(AdminAuthExtensions.MicrosoftScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration
                {
                    Issuer = Issuer,
                    AuthorizationEndpoint = AuthorizeEndpoint,
                    TokenEndpoint = TokenEndpoint,
                };
                configuration.SigningKeys.Add(provider.SigningKey);
                options.Configuration = configuration;
                // The framework builds a metadata downloader from the authority; replace it by the static document.
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                options.Backchannel = new HttpClient(provider.TokenEndpointHandler);
            });
        });
    }

    /// <summary>
    /// A cookie-keeping, non-redirecting client with an address of its own. The base address is https, so that the
    /// Secure correlation and nonce cookies of the OpenID Connect handler are kept.
    /// </summary>
    public HttpClient CreateSsoBrowser() => WithOwnHttpsAddress(this);

    public static HttpClient WithOwnHttpsAddress(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });
        var n = Interlocked.Increment(ref _clientCounter);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.{n / 65536 % 256}.{n / 256 % 256}.{n % 256}");
        return client;
    }
}

/// <summary>What the fake identity provider puts into the ID token it returns for one authorization code.</summary>
public sealed record FakeIdToken(
    string Nonce,
    string Tid = MicrosoftSsoWebAppFactory.TenantId,
    string? Oid = "44444444-4444-4444-4444-444444444444",
    string? PreferredUsername = "alice@contoso.test",
    string? Name = "Alice Example",
    string Issuer = "",
    string Audience = MicrosoftSsoWebAppFactory.ClientId);

/// <summary>A test double for Microsoft Entra ID: an RSA signing key and a token endpoint.</summary>
public sealed class FakeIdentityProvider
{
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly ConcurrentDictionary<string, FakeIdToken> _codes = new();

    public FakeIdentityProvider()
    {
        SigningKey = new RsaSecurityKey(_rsa) { KeyId = "test-key" };
        TokenEndpointHandler = new TokenHandler(this);
    }

    public RsaSecurityKey SigningKey { get; }

    public HttpMessageHandler TokenEndpointHandler { get; }

    /// <summary>The form fields of the last request to the token endpoint.</summary>
    public IReadOnlyDictionary<string, string> LastTokenRequest { get; private set; } = new Dictionary<string, string>();

    /// <summary>Makes <paramref name="code"/> redeemable for an ID token with these claims.</summary>
    public void Register(string code, FakeIdToken token) => _codes[code] = token;

    /// <summary>Signs an ID token like Entra ID would (v2.0, tenant-specific issuer unless overridden).</summary>
    public string CreateIdToken(FakeIdToken spec)
    {
        var claims = new Dictionary<string, object>
        {
            ["tid"] = spec.Tid,
            ["nonce"] = spec.Nonce,
            ["sub"] = "pairwise-subject",
        };
        if (spec.Oid is not null)
        {
            claims["oid"] = spec.Oid;
        }

        if (spec.PreferredUsername is not null)
        {
            claims["preferred_username"] = spec.PreferredUsername;
        }

        if (spec.Name is not null)
        {
            claims["name"] = spec.Name;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = string.IsNullOrEmpty(spec.Issuer) ? MicrosoftSsoWebAppFactory.Issuer : spec.Issuer,
            Audience = spec.Audience,
            Claims = claims,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private sealed class TokenHandler(FakeIdentityProvider owner) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsoluteUri != MicrosoftSsoWebAppFactory.TokenEndpoint || request.Method != HttpMethod.Post)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var form = body.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(pair => pair.Split('=', 2))
                .ToDictionary(p => WebUtility.UrlDecode(p[0]), p => WebUtility.UrlDecode(p.Length > 1 ? p[1] : string.Empty));
            owner.LastTokenRequest = form;

            if (!form.TryGetValue("code", out var code) || !owner._codes.TryGetValue(code, out var spec))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("""{"error":"invalid_grant"}""", Encoding.UTF8, "application/json"),
                };
            }

            var json = JsonSerializer.Serialize(new
            {
                token_type = "Bearer",
                scope = "openid profile email",
                expires_in = 3600,
                access_token = "fake-access-token",
                id_token = owner.CreateIdToken(spec),
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
