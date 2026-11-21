using System.Net;
using System.Security.Claims;
using System.Web;
using CxmlDevServer.Web.Security;
using CxmlDevServer.Web.Tests.Support;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Web.Tests;

/// <summary>The settings of the Microsoft 365 sign-in and the rules for an enabled login.</summary>
public sealed class MicrosoftSignInOptionsTests
{
    private const string Tenant = MicrosoftSsoWebAppFactory.TenantId;
    private const string Client = MicrosoftSsoWebAppFactory.ClientId;

    private static AdminOptions Microsoft(Action<MicrosoftSignInOptions>? tweak = null)
    {
        var options = new AdminOptions
        {
            Enabled = true,
            Microsoft = { TenantId = Tenant, ClientId = Client, ClientSecret = "s3cret" },
        };
        tweak?.Invoke(options.Microsoft);
        return options;
    }

    [Fact]
    public void Microsoft_alone_is_a_valid_way_to_sign_in()
    {
        Assert.Null(Microsoft().Validate());
    }

    [Fact]
    public void A_password_alone_is_still_valid_and_both_together_are_valid()
    {
        var hash = AdminCredentialValidator.Hash("pw");
        Assert.Null(new AdminOptions { Enabled = true, PasswordHash = hash }.Validate());
        Assert.Null(new AdminOptions { Enabled = true, Password = "pw" }.Validate());

        var both = Microsoft();
        both.PasswordHash = hash;
        Assert.Null(both.Validate());
    }

    [Fact]
    public void Without_any_sign_in_method_the_message_names_the_password_and_the_microsoft_settings()
    {
        var problem = new AdminOptions { Enabled = true }.Validate();

        Assert.NotNull(problem);
        Assert.Contains("Admin__PasswordHash", problem);
        Assert.Contains("Admin__Microsoft__TenantId", problem);
        Assert.Contains("Admin__Microsoft__ClientSecret", problem);
    }

    [Fact]
    public void A_disabled_login_ignores_incomplete_microsoft_settings()
    {
        var options = new AdminOptions { Enabled = false, Microsoft = { TenantId = "common" } };

        Assert.Null(options.Validate());
    }

    [Theory]
    [InlineData("common")]
    [InlineData("organizations")]
    [InlineData("consumers")]
    [InlineData("Common")]
    [InlineData("contoso.onmicrosoft.com")]
    [InlineData("not-a-guid")]
    public void Only_a_single_tenant_guid_is_accepted(string tenant)
    {
        var problem = Microsoft(m => m.TenantId = tenant).Validate();

        Assert.NotNull(problem);
        Assert.Contains("Admin:Microsoft:TenantId", problem);
    }

    [Fact]
    public void Every_microsoft_setting_is_checked_once_one_of_them_is_set()
    {
        Assert.Contains("ClientSecret", Microsoft(m => m.ClientSecret = " ").Validate());
        Assert.Contains("ClientId", Microsoft(m => m.ClientId = "nope").Validate());
        Assert.Contains("ClientId", Microsoft(m => m.ClientId = null).Validate());
        Assert.Contains("TenantId", Microsoft(m => m.TenantId = null).Validate());
        Assert.Contains("Instance", Microsoft(m => m.Instance = "http://login.example").Validate());
        Assert.Contains("CallbackPath", Microsoft(m => m.CallbackPath = "signin-oidc").Validate());

        // A lone tenant id is not silently ignored.
        var onlyTenant = new AdminOptions { Enabled = true, PasswordHash = AdminCredentialValidator.Hash("pw"), Microsoft = { TenantId = Tenant } };
        Assert.Contains("ClientId", onlyTenant.Validate());
    }

    [Fact]
    public void Empty_microsoft_settings_mean_the_sign_in_is_off()
    {
        var options = new AdminOptions
        {
            Enabled = true,
            PasswordHash = AdminCredentialValidator.Hash("pw"),
            Microsoft = { TenantId = "", ClientId = "  " },
        };

        Assert.False(options.Microsoft.IsConfigured);
        Assert.Null(options.Validate());
    }

    [Fact]
    public void The_authority_is_the_tenant_specific_v2_endpoint()
    {
        Assert.Equal($"https://login.microsoftonline.com/{Tenant}/v2.0", Microsoft().Microsoft.Authority);
        Assert.Equal($"https://login.microsoftonline.us/{Tenant}/v2.0", Microsoft(m => m.Instance = "https://login.microsoftonline.us").Microsoft.Authority);
    }

    [Fact]
    public void Without_a_password_the_password_login_never_succeeds_not_even_with_empty_credentials()
    {
        var validator = new AdminCredentialValidator(Options.Create(Microsoft()));

        Assert.False(validator.IsValid("admin", string.Empty));
        Assert.False(validator.IsValid("admin", null));
    }
}

/// <summary>The claims of the cookie principal that is created from a Microsoft ID token.</summary>
public sealed class MicrosoftClaimsTests
{
    private const string Tenant = MicrosoftSsoWebAppFactory.TenantId;

    private static ClaimsPrincipal IdToken(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test"));

    [Fact]
    public void The_name_is_the_preferred_username_then_the_email_then_the_display_name_then_the_object_id()
    {
        (string, string) tid = ("tid", Tenant), oid = ("oid", "obj-1");

        Assert.Equal("a@x.test", Name(tid, oid, ("preferred_username", "a@x.test"), ("email", "b@x.test"), ("name", "Alice")));
        Assert.Equal("b@x.test", Name(tid, oid, ("email", "b@x.test"), ("name", "Alice")));
        Assert.Equal("Alice", Name(tid, oid, ("name", "Alice")));
        Assert.Equal("obj-1", Name(tid, oid));
        Assert.Equal("a@x.test", Name(tid, oid, ("preferred_username", "a@x.test"), ("name", " ")));
    }

    [Fact]
    public void Only_the_needed_claims_end_up_in_the_cookie()
    {
        var principal = MicrosoftClaims.CreatePrincipal(
            IdToken(("tid", Tenant), ("oid", "obj-1"), ("preferred_username", "a@x.test"), ("name", "Alice"), ("sub", "s"), ("aio", "huge-blob")),
            Tenant)!;

        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.Equal("a@x.test", principal.Identity.Name);
        Assert.Equal("Alice", principal.FindFirst("name")!.Value);
        Assert.Equal("obj-1", principal.FindFirst("oid")!.Value);
        Assert.Equal(Tenant, principal.FindFirst("tid")!.Value);
        Assert.Equal("microsoft", principal.FindFirst(AdminAuthExtensions.AuthMethodClaim)!.Value);
        Assert.Null(principal.FindFirst("aio"));
        Assert.Null(principal.FindFirst("sub"));
    }

    [Fact]
    public void A_token_without_an_object_id_is_refused()
    {
        Assert.Null(MicrosoftClaims.CreatePrincipal(IdToken(("tid", Tenant), ("preferred_username", "a@x.test")), Tenant));
        Assert.Null(MicrosoftClaims.CreatePrincipal(IdToken(("tid", Tenant), ("oid", " ")), Tenant));
    }

    [Theory]
    [InlineData(MicrosoftSsoWebAppFactory.OtherTenantId)]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public void A_token_of_another_tenant_is_refused(string tid)
    {
        Assert.Null(MicrosoftClaims.CreatePrincipal(IdToken(("tid", tid), ("oid", "obj-1")), Tenant));
    }

    [Fact]
    public void A_token_without_a_tenant_is_refused_and_the_tenant_comparison_ignores_case()
    {
        Assert.Null(MicrosoftClaims.CreatePrincipal(IdToken(("oid", "obj-1")), Tenant));
        Assert.NotNull(MicrosoftClaims.CreatePrincipal(IdToken(("tid", Tenant.ToUpperInvariant()), ("oid", "obj-1")), Tenant.ToLowerInvariant()));
    }

    private static string Name(params (string, string)[] claims) =>
        MicrosoftClaims.CreatePrincipal(IdToken(claims), Tenant)!.Identity!.Name!;
}

/// <summary>The Microsoft 365 sign-in end to end, against a fake identity provider (no network).</summary>
public sealed class MicrosoftSsoTests : IClassFixture<MicrosoftSsoWebAppFactory>
{
    private readonly MicrosoftSsoWebAppFactory _factory;

    public MicrosoftSsoTests(MicrosoftSsoWebAppFactory factory) => _factory = factory;

    private static async Task<HttpResponseMessage> PostMicrosoftLoginAsync(HttpClient browser, string? returnUrl = null, bool withToken = true)
    {
        var loginPage = await browser.GetStringAsync("/login");
        var fields = new List<KeyValuePair<string, string>>();
        if (returnUrl is not null)
        {
            fields.Add(new("returnUrl", returnUrl));
        }

        if (withToken)
        {
            fields.Add(new("__RequestVerificationToken", Http.ExtractAntiforgeryToken(loginPage)));
        }

        return await browser.PostAsync("/account/login/microsoft", new FormUrlEncodedContent(fields));
    }

    private static System.Collections.Specialized.NameValueCollection QueryOf(HttpResponseMessage redirect) =>
        HttpUtility.ParseQueryString(redirect.Headers.Location!.Query);

    /// <summary>Starts the sign-in; returns the authorize URL's state and nonce.</summary>
    private static async Task<(string State, string Nonce)> StartAsync(HttpClient browser, string? returnUrl = null)
    {
        var redirect = await PostMicrosoftLoginAsync(browser, returnUrl);
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        var query = QueryOf(redirect);
        return (query["state"]!, query["nonce"]!);
    }

    private Task<HttpResponseMessage> CallbackAsync(HttpClient browser, string state, string code) =>
        browser.PostAsync("/signin-oidc", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("state", state),
        ]));

    private static string NewCode() => "code-" + Guid.NewGuid().ToString("N");

    private static void AssertNoAdminCookie(HttpResponseMessage response) =>
        Assert.DoesNotContain(response.Headers, h => h.Key == "Set-Cookie" && h.Value.Any(v => v.StartsWith("cxml-dev-admin=")));

    // ---- the login page ----

    [Fact]
    public async Task The_login_page_offers_only_microsoft_when_no_password_is_set()
    {
        var html = await _factory.CreateSsoBrowser().GetStringAsync("/login");

        Assert.Contains("id=\"microsoft-login\"", html);
        Assert.Contains("action=\"account/login/microsoft\"", html);
        Assert.Contains("Sign in with Microsoft", html);
        Assert.Contains("__RequestVerificationToken", html);
        Assert.DoesNotContain("name=\"password\"", html);
        Assert.DoesNotContain("login-divider", html);
    }

    [Fact]
    public async Task The_login_page_offers_both_when_a_password_is_set_too()
    {
        using var both = _factory.WithWebHostBuilder(b => b.UseSetting("Admin:PasswordHash", AdminWebAppFactory.PasswordHash));

        var html = await MicrosoftSsoWebAppFactory.WithOwnHttpsAddress(both).GetStringAsync("/login");

        Assert.Contains("id=\"microsoft-login\"", html);
        Assert.Contains("name=\"password\"", html);
        Assert.Contains("login-divider", html);
    }

    [Fact]
    public async Task The_login_page_of_a_password_only_setup_has_no_microsoft_button()
    {
        using var passwordOnly = new AdminWebAppFactory();

        var html = await passwordOnly.CreateBrowserWithOwnAddress().GetStringAsync("/login");

        Assert.DoesNotContain("microsoft-login", html);
        Assert.Contains("name=\"password\"", html);
        Assert.DoesNotContain("login-divider", html);
    }

    [Fact]
    public async Task A_failed_microsoft_sign_in_shows_its_own_message()
    {
        var html = await _factory.CreateSsoBrowser().GetStringAsync("/login?error=sso");

        Assert.Contains("id=\"login-error\"", html);
        Assert.Contains("Microsoft sign-in failed or was cancelled", html);
        Assert.DoesNotContain("Wrong user name or password", html);
    }

    // ---- starting the sign-in ----

    [Fact]
    public async Task Starting_the_sign_in_needs_an_antiforgery_token()
    {
        var response = await PostMicrosoftLoginAsync(_factory.CreateSsoBrowser(), "/partners", withToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Starting_the_sign_in_redirects_to_the_tenant_with_authorization_code_flow_and_pkce()
    {
        var response = await PostMicrosoftLoginAsync(_factory.CreateSsoBrowser(), "/partners");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal(MicrosoftSsoWebAppFactory.AuthorizeEndpoint, location.GetLeftPart(UriPartial.Path));
        var query = QueryOf(response);
        Assert.Equal(MicrosoftSsoWebAppFactory.ClientId, query["client_id"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.False(string.IsNullOrEmpty(query["code_challenge"]));
        Assert.Equal("select_account", query["prompt"]);
        Assert.Equal("https://localhost/signin-oidc", query["redirect_uri"]);
        var scopes = query["scope"]!.Split(' ');
        Assert.Contains("openid", scopes);
        Assert.Contains("profile", scopes);
        Assert.Contains("email", scopes);
        Assert.False(string.IsNullOrEmpty(query["state"]));
        Assert.False(string.IsNullOrEmpty(query["nonce"]));
        // The correlation and nonce cookies are set for the callback.
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".AspNetCore.Correlation."));
        AssertNoAdminCookie(response);
    }

    [Theory]
    [InlineData("/partners", "/partners")]
    [InlineData("/sender?template=order-request", "/sender?template=order-request")]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example/phish", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("/account/logout", "/")]
    [InlineData("/login", "/")]
    [InlineData(null, "/")]
    public async Task The_return_url_is_sanitized_before_it_goes_into_the_state(string? returnUrl, string expected)
    {
        var (state, _) = await StartAsync(_factory.CreateSsoBrowser(), returnUrl);

        var options = _factory.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(AdminAuthExtensions.MicrosoftScheme);
        var properties = options.StateDataFormat.Unprotect(state);
        Assert.NotNull(properties);
        Assert.Equal(expected, properties.RedirectUri);
    }

    // ---- the callback ----

    [Fact]
    public async Task A_user_of_the_tenant_signs_in_and_returns_to_the_requested_page()
    {
        var browser = _factory.CreateSsoBrowser();
        var (state, nonce) = await StartAsync(browser, "/partners");
        var code = NewCode();
        _factory.IdentityProvider.Register(code, new FakeIdToken(nonce));

        var callback = await CallbackAsync(browser, state, code);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/partners", callback.Headers.Location!.OriginalString);
        var cookie = Assert.Single(callback.Headers.GetValues("Set-Cookie"), v => v.StartsWith("cxml-dev-admin="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        // The code is redeemed with the client secret, PKCE verifier and the same redirect URI as the request.
        var tokenRequest = _factory.IdentityProvider.LastTokenRequest;
        Assert.Equal(MicrosoftSsoWebAppFactory.ClientSecret, tokenRequest["client_secret"]);
        Assert.Equal(MicrosoftSsoWebAppFactory.ClientId, tokenRequest["client_id"]);
        Assert.Equal("authorization_code", tokenRequest["grant_type"]);
        Assert.Equal("https://localhost/signin-oidc", tokenRequest["redirect_uri"]);
        Assert.False(string.IsNullOrEmpty(tokenRequest["code_verifier"]));

        var partners = await browser.GetAsync("/partners");
        Assert.Equal(HttpStatusCode.OK, partners.StatusCode);
        var html = await partners.Content.ReadAsStringAsync();
        Assert.Contains("Trading partners", html);
        Assert.Contains("id=\"nav-username\"", html);
        Assert.Contains(">alice@contoso.test<", html);
        Assert.Contains("action=\"account/logout\"", html);
    }

    [Fact]
    public async Task Logging_out_ends_the_local_session_only()
    {
        var browser = _factory.CreateSsoBrowser();
        var (state, nonce) = await StartAsync(browser);
        var code = NewCode();
        _factory.IdentityProvider.Register(code, new FakeIdToken(nonce));
        await CallbackAsync(browser, state, code);
        var token = Http.ExtractAntiforgeryToken(await browser.GetStringAsync("/partners"));

        var logout = await browser.PostAsync("/account/logout", new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("__RequestVerificationToken", token)]));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        // A local redirect to the login page, not to the identity provider's logout endpoint.
        Assert.Equal("/login", logout.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/partners")).StatusCode);
    }

    [Fact]
    public async Task An_account_of_another_tenant_is_refused_even_with_a_valid_signature_and_issuer()
    {
        var browser = _factory.CreateSsoBrowser();
        var (state, nonce) = await StartAsync(browser, "/partners");
        var code = NewCode();
        // The issuer and signature are fine; only the tid claim is of another tenant (second guard).
        _factory.IdentityProvider.Register(code, new FakeIdToken(nonce, Tid: MicrosoftSsoWebAppFactory.OtherTenantId));

        var callback = await CallbackAsync(browser, state, code);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/login?error=sso", callback.Headers.Location!.OriginalString);
        AssertNoAdminCookie(callback);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/partners")).StatusCode);
    }

    [Fact]
    public async Task A_token_issued_by_another_tenant_is_refused_by_the_issuer_check()
    {
        var browser = _factory.CreateSsoBrowser();
        var (state, nonce) = await StartAsync(browser);
        var code = NewCode();
        _factory.IdentityProvider.Register(code, new FakeIdToken(
            nonce,
            Tid: MicrosoftSsoWebAppFactory.OtherTenantId,
            Issuer: $"https://login.microsoftonline.com/{MicrosoftSsoWebAppFactory.OtherTenantId}/v2.0"));

        var callback = await CallbackAsync(browser, state, code);

        Assert.Equal("/login?error=sso", callback.Headers.Location!.OriginalString);
        AssertNoAdminCookie(callback);
    }

    [Fact]
    public async Task A_token_for_another_application_is_refused()
    {
        var browser = _factory.CreateSsoBrowser();
        var (state, nonce) = await StartAsync(browser);
        var code = NewCode();
        _factory.IdentityProvider.Register(code, new FakeIdToken(nonce, Audience: "99999999-9999-9999-9999-999999999999"));

        var callback = await CallbackAsync(browser, state, code);

        Assert.Equal("/login?error=sso", callback.Headers.Location!.OriginalString);
        AssertNoAdminCookie(callback);
    }

    [Fact]
    public async Task A_token_with_the_wrong_nonce_or_without_an_object_id_is_refused()
    {
        foreach (var spec in new Func<string, FakeIdToken>[]
                 {
                     nonce => new FakeIdToken("not-" + nonce),
                     nonce => new FakeIdToken(nonce, Oid: null),
                 })
        {
            var browser = _factory.CreateSsoBrowser();
            var (state, nonce) = await StartAsync(browser);
            var code = NewCode();
            _factory.IdentityProvider.Register(code, spec(nonce));

            var callback = await CallbackAsync(browser, state, code);

            Assert.Equal("/login?error=sso", callback.Headers.Location!.OriginalString);
            AssertNoAdminCookie(callback);
        }
    }

    [Fact]
    public async Task A_callback_with_a_bad_state_redirects_to_the_login_page_without_details()
    {
        var browser = _factory.CreateSsoBrowser();

        var callback = await CallbackAsync(browser, "garbage", "x");

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/login?error=sso", callback.Headers.Location!.OriginalString);
        AssertNoAdminCookie(callback);
        Assert.Equal(string.Empty, await callback.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_cancelled_sign_in_redirects_to_the_login_page()
    {
        var browser = _factory.CreateSsoBrowser();
        var (state, _) = await StartAsync(browser);

        var callback = await browser.PostAsync("/signin-oidc", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("error", "access_denied"),
            new KeyValuePair<string, string>("error_description", "AADSTS65004: user declined"),
            new KeyValuePair<string, string>("state", state),
        ]));

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/login?error=sso", callback.Headers.Location!.OriginalString);
        AssertNoAdminCookie(callback);
    }

    [Fact]
    public async Task A_state_cannot_be_used_from_another_browser()
    {
        var victim = _factory.CreateSsoBrowser();
        var (state, nonce) = await StartAsync(victim);
        var code = NewCode();
        _factory.IdentityProvider.Register(code, new FakeIdToken(nonce));

        // The attacker has no correlation cookie of the victim's flow.
        var callback = await CallbackAsync(_factory.CreateSsoBrowser(), state, code);

        Assert.Equal("/login?error=sso", callback.Headers.Location!.OriginalString);
        AssertNoAdminCookie(callback);
    }

    // ---- other setups ----

    [Fact]
    public async Task The_password_login_cannot_be_used_when_only_microsoft_is_configured()
    {
        var browser = _factory.CreateSsoBrowser();
        var token = Http.ExtractAntiforgeryToken(await browser.GetStringAsync("/login"));

        var response = await browser.PostAsync("/account/login", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", string.Empty),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        ]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/login?error=1", response.Headers.Location!.OriginalString);
        AssertNoAdminCookie(response);
    }

    [Fact]
    public async Task The_password_login_still_works_next_to_microsoft()
    {
        using var both = _factory.WithWebHostBuilder(b => b.UseSetting("Admin:PasswordHash", AdminWebAppFactory.PasswordHash));
        var browser = MicrosoftSsoWebAppFactory.WithOwnHttpsAddress(both);
        var token = Http.ExtractAntiforgeryToken(await browser.GetStringAsync("/login"));

        var response = await browser.PostAsync("/account/login", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", AdminWebAppFactory.Password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        ]));

        Assert.Equal("/", response.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/partners")).StatusCode);
    }

    [Fact]
    public async Task With_the_admin_login_disabled_the_microsoft_login_goes_to_the_dashboard()
    {
        using var disabled = _factory.WithWebHostBuilder(b => b.UseSetting("Admin:Enabled", "false"));
        var browser = MicrosoftSsoWebAppFactory.WithOwnHttpsAddress(disabled);

        var response = await browser.PostAsync("/account/login/microsoft", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/partners")).StatusCode);
    }

    [Fact]
    public async Task Without_microsoft_settings_the_microsoft_login_and_callback_are_inert()
    {
        using var passwordOnly = new AdminWebAppFactory();
        var browser = passwordOnly.CreateBrowserWithOwnAddress();

        var login = await browser.PostAsync("/account/login/microsoft", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/login", login.Headers.Location!.OriginalString);

        // The scheme is registered but never challenged; a stray callback is just a failed sign-in.
        var callback = await browser.PostAsync("/signin-oidc", new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("code", "x"), new KeyValuePair<string, string>("state", "y")]));
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/login?error=sso", callback.Headers.Location!.OriginalString);
        AssertNoAdminCookie(callback);
    }

    [Fact]
    public async Task The_public_base_url_decides_the_redirect_uri_of_the_request_and_of_the_code_redemption()
    {
        using var proxied = _factory.WithWebHostBuilder(b => b.UseSetting("Cxml:PublicBaseUrl", "https://dev.example.test/"));
        var browser = MicrosoftSsoWebAppFactory.WithOwnHttpsAddress(proxied);
        var response = await PostMicrosoftLoginAsync(browser);
        var query = QueryOf(response);
        var (state, nonce, redirectUri) = (query["state"]!, query["nonce"]!, query["redirect_uri"]!);
        var code = NewCode();
        _factory.IdentityProvider.Register(code, new FakeIdToken(nonce));

        var callback = await CallbackAsync(browser, state, code);

        Assert.Equal("https://dev.example.test/signin-oidc", redirectUri);
        Assert.Equal(redirectUri, _factory.IdentityProvider.LastTokenRequest["redirect_uri"]);
        Assert.Contains(callback.Headers.GetValues("Set-Cookie"), v => v.StartsWith("cxml-dev-admin="));
    }

    [Fact]
    public async Task Buyer_facing_endpoints_stay_anonymous_with_the_microsoft_sign_in()
    {
        var client = _factory.CreateSsoBrowser();

        Assert.Equal("OK", await client.GetStringAsync("/health"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/cxml")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/login")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/partners")).StatusCode);
    }
}
