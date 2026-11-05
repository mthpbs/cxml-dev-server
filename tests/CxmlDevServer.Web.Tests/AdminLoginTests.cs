using System.Net;
using System.Reflection;
using System.Text;
using Cxml.Protocol.Documents.PunchOut;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.PunchOut;
using CxmlDevServer.Web.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Web.Tests;

public sealed class AdminLoginTests : IClassFixture<AdminWebAppFactory>
{
    private readonly AdminWebAppFactory _factory;

    public AdminLoginTests(AdminWebAppFactory factory) => _factory = factory;

    private static async Task<HttpResponseMessage> PostLoginAsync(
        HttpClient browser, string username, string password, string? returnUrl = null, bool withToken = true)
    {
        var loginPage = await browser.GetStringAsync("/login");
        var fields = new List<KeyValuePair<string, string>>
        {
            new("username", username),
            new("password", password),
        };
        if (returnUrl is not null)
        {
            fields.Add(new("returnUrl", returnUrl));
        }

        if (withToken)
        {
            fields.Add(new("__RequestVerificationToken", Http.ExtractAntiforgeryToken(loginPage)));
        }

        return await browser.PostAsync("/account/login", new FormUrlEncodedContent(fields));
    }

    private static async Task<HttpClient> SignedInAsync(AdminWebAppFactory factory)
    {
        var browser = factory.CreateBrowserWithOwnAddress();
        var response = await PostLoginAsync(browser, AdminWebAppFactory.Username, AdminWebAppFactory.Password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return browser;
    }

    // ---- the admin UI is protected ----

    [Theory]
    [InlineData("/", "%2F")]
    [InlineData("/partners", "%2Fpartners")]
    [InlineData("/messages", "%2Fmessages")]
    [InlineData("/sender?template=order-request", "%2Fsender%3Ftemplate%3Dorder-request")]
    [InlineData("/catalog/new", "%2Fcatalog%2Fnew")]
    public async Task Admin_pages_redirect_to_the_login_page(string path, string expectedReturnUrl)
    {
        var response = await _factory.CreateBrowserWithOwnAddress().GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.StartsWith("/login?", location);
        Assert.Contains("returnUrl=" + expectedReturnUrl, location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_login_page_is_static_and_anonymous()
    {
        var response = await _factory.CreateBrowserWithOwnAddress().GetAsync("/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("action=\"account/login\"", html);
        Assert.Contains("name=\"password\"", html);
        Assert.Contains("__RequestVerificationToken", html);
        // Static rendering: no Blazor circuit is started for this page, and no admin menu is shown.
        Assert.DoesNotContain("Trading partners", html);
        Assert.DoesNotContain("<!--Blazor:{", html);
    }

    [Fact]
    public void Every_routable_page_declares_whether_it_needs_the_admin_login()
    {
        var anonymousPages = new[] { "Shop", "NotFound", "Error", "Login" };
        var pages = typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t) && t.GetCustomAttributes<RouteAttribute>().Any())
            .ToList();
        Assert.True(pages.Count >= 18);

        foreach (var page in pages)
        {
            var anonymous = page.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
            var authorize = page.GetCustomAttributes<AuthorizeAttribute>().SingleOrDefault();
            if (anonymousPages.Contains(page.Name))
            {
                Assert.True(anonymous, $"{page.Name} must be anonymous.");
            }
            else
            {
                Assert.False(anonymous, $"{page.Name} must not be anonymous.");
                Assert.Equal("Admin", authorize?.Policy);
            }
        }
    }

    [Fact]
    public async Task The_admin_policy_rejects_anonymous_users_and_accepts_the_signed_in_admin()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var admin = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin")], "Cookies"));

        Assert.False((await authorization.AuthorizeAsync(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()), null, "Admin")).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(admin, null, "Admin")).Succeeded);
    }

    // ---- buyer-facing endpoints stay anonymous ----

    [Fact]
    public async Task Health_cxml_ping_and_static_assets_need_no_login()
    {
        var client = _factory.CreateBrowserWithOwnAddress();

        Assert.Equal("OK", await client.GetStringAsync("/health"));
        var ping = await client.GetAsync("/cxml");
        Assert.Equal(HttpStatusCode.OK, ping.StatusCode);
        Assert.Contains("cXML Dev Server is running", await ping.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/app.css")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_framework/blazor.web.js")).StatusCode);
    }

    [Fact]
    public async Task Posting_the_specification_order_needs_no_login()
    {
        var xml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Samples", "0-OrderRequest.xml"));
        xml = xml.Replace("DO1234", "DO-ADMIN-ON");

        var response = await _factory.CreateBrowserWithOwnAddress().PostCxmlAsync(xml);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("code=\"200\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_complete_punchout_round_trip_works_without_logging_in()
    {
        var setup = await _factory.RenderTemplateAsync("punchout-setup-create");
        var buyer = _factory.CreateBrowserWithOwnAddress();

        // Setup request from the procurement system.
        var setupResponse = await buyer.PostCxmlAsync(setup);
        var envelope = _factory.Read(await setupResponse.Content.ReadAsStringAsync());
        Assert.Equal(200, envelope.Response?.Status.Code);
        var startPage = Assert.IsType<PunchOutSetupResponse>(envelope.Response!.Payload).StartPageUrl;
        var token = startPage[(startPage.LastIndexOf('/') + 1)..];
        var buyerCookie = Assert.IsType<PunchOutSetupRequest>(_factory.Read(setup).Request!.Payload).BuyerCookie;

        // The user's browser opens the StartPage (an interactive Blazor page) - anonymous, no redirect.
        var browser = _factory.CreateBrowserWithOwnAddress();
        var shop = await browser.GetAsync(startPage);
        Assert.Equal(HttpStatusCode.OK, shop.StatusCode);
        var shopHtml = await shop.Content.ReadAsStringAsync();
        Assert.Contains("Dev Supplier Catalog", shopHtml);
        Assert.DoesNotContain("Trading partners", shopHtml);
        var antiforgery = Http.ExtractAntiforgeryToken(shopHtml);

        await _factory.WithScopeAsync(async sp =>
        {
            await sp.GetRequiredService<PunchOutService>().AddToCartAsync(token, 1, 2);
            return 0;
        });

        // Checkout: an antiforgery protected POST, also anonymous.
        var checkout = await browser.PostAsync($"/punchout/{token}/checkout", new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery)]));
        Assert.Equal(HttpStatusCode.OK, checkout.StatusCode);
        var (_, field, value) = Http.ParseReturnForm(await checkout.Content.ReadAsStringAsync());

        // The browser posts the cart to the built-in test buyer.
        var received = await browser.PostAsync("/test-buyer/punchout-return", new FormUrlEncodedContent(
            [new KeyValuePair<string, string>(field, value)]));
        Assert.Equal(HttpStatusCode.OK, received.StatusCode);
        var receivedHtml = await received.Content.ReadAsStringAsync();
        Assert.Contains("Valid PunchOutOrderMessage", receivedHtml);
        Assert.Contains(buyerCookie, receivedHtml);
        var session = await _factory.QueryAsync(db => db.PunchOutSessions.AsNoTracking().SingleAsync(s => s.Token == token));
        Assert.Equal(PunchOutSessionStatus.Submitted, session.Status);
    }

    // ---- signing in and out ----

    [Fact]
    public async Task Wrong_credentials_are_rejected_and_nothing_is_unlocked()
    {
        var browser = _factory.CreateBrowserWithOwnAddress();

        var wrongPassword = await PostLoginAsync(browser, "admin", "nope", "/partners");
        var wrongUser = await PostLoginAsync(browser, "root", AdminWebAppFactory.Password);

        foreach (var response in new[] { wrongPassword, wrongUser })
        {
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/login?error=1", response.Headers.Location!.OriginalString);
            Assert.DoesNotContain(response.Headers, h => h.Key == "Set-Cookie" && h.Value.Any(v => v.StartsWith("cxml-dev-admin=")));
        }

        Assert.Contains("returnUrl=%2Fpartners", wrongPassword.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/partners")).StatusCode);
        var errorPage = await browser.GetStringAsync(wrongPassword.Headers.Location.OriginalString);
        Assert.Contains("Wrong user name or password", errorPage);
    }

    [Fact]
    public async Task The_right_password_signs_in_and_returns_to_the_requested_page()
    {
        var browser = _factory.CreateBrowserWithOwnAddress();

        var login = await PostLoginAsync(browser, AdminWebAppFactory.Username, AdminWebAppFactory.Password, "/partners");

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/partners", login.Headers.Location!.OriginalString);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), v => v.StartsWith("cxml-dev-admin="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        // SecurePolicy SameAsRequest: this request is plain http, so the cookie is not marked secure.
        Assert.DoesNotContain("; secure", cookie, StringComparison.OrdinalIgnoreCase);

        var partners = await browser.GetAsync("/partners");
        Assert.Equal(HttpStatusCode.OK, partners.StatusCode);
        var html = await partners.Content.ReadAsStringAsync();
        Assert.Contains("Trading partners", html);
        // The sidebar shows who is signed in, with a sign-out button.
        Assert.Contains("id=\"nav-username\"", html);
        Assert.Contains(">admin<", html);
        Assert.Contains("action=\"account/logout\"", html);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task A_plain_text_password_works_as_a_fallback_and_the_hash_wins_when_both_are_set()
    {
        using var plainOnly = _factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Admin:PasswordHash", string.Empty);
            b.UseSetting("Admin:Password", "plain-secret");
        });
        var browser = AdminWebAppFactory.WithOwnAddress(plainOnly);
        Assert.StartsWith("/login?error=1", (await PostLoginAsync(browser, "admin", AdminWebAppFactory.Password)).Headers.Location!.OriginalString);
        var ok = await PostLoginAsync(browser, "admin", "plain-secret");
        Assert.Equal("/", ok.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/partners")).StatusCode);

        using var both = _factory.WithWebHostBuilder(b => b.UseSetting("Admin:Password", "plain-secret"));
        var bothBrowser = AdminWebAppFactory.WithOwnAddress(both);
        Assert.StartsWith("/login?error=1", (await PostLoginAsync(bothBrowser, "admin", "plain-secret")).Headers.Location!.OriginalString);
        Assert.Equal("/", (await PostLoginAsync(bothBrowser, "admin", AdminWebAppFactory.Password)).Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var browser = await SignedInAsync(_factory);
        var page = await browser.GetStringAsync("/partners");
        var token = Http.ExtractAntiforgeryToken(page);

        var logout = await browser.PostAsync("/account/logout", new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("__RequestVerificationToken", token)]));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/login", logout.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/partners")).StatusCode);
    }

    [Fact]
    public async Task Login_and_logout_refuse_a_post_without_an_antiforgery_token()
    {
        var browser = await SignedInAsync(_factory);

        var logout = await browser.PostAsync("/account/logout", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, logout.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/partners")).StatusCode);

        var anonymous = _factory.CreateBrowserWithOwnAddress();
        var login = await PostLoginAsync(anonymous, AdminWebAppFactory.Username, AdminWebAppFactory.Password, withToken: false);
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/partners")).StatusCode);
    }

    [Theory]
    [InlineData("https://evil.example/phish")]
    [InlineData("//evil.example/phish")]
    [InlineData("/\\evil.example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("evil")]
    [InlineData("/login?returnUrl=/login")]
    public async Task A_return_url_that_is_not_local_is_ignored(string returnUrl)
    {
        var browser = _factory.CreateBrowserWithOwnAddress();

        var login = await PostLoginAsync(browser, AdminWebAppFactory.Username, AdminWebAppFactory.Password, returnUrl);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Too_many_login_attempts_from_one_address_are_rejected_with_429()
    {
        var browser = _factory.CreateBrowserWithOwnAddress();
        var loginPage = await browser.GetStringAsync("/login");
        var token = Http.ExtractAntiforgeryToken(loginPage);
        HttpContent Form(string password) => new FormUrlEncodedContent(
        [
            new("username", "admin"), new("password", password), new("__RequestVerificationToken", token),
        ]);

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Redirect, (await browser.PostAsync("/account/login", Form("wrong"))).StatusCode);
        }

        // Even the right password is refused once the limit is hit ...
        Assert.Equal(HttpStatusCode.TooManyRequests, (await browser.PostAsync("/account/login", Form(AdminWebAppFactory.Password))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/partners")).StatusCode);

        // ... while another address is not affected.
        var other = await SignedInAsync(_factory);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/partners")).StatusCode);
    }

    [Fact]
    public async Task The_data_protection_keys_are_written_to_the_configured_folder()
    {
        await SignedInAsync(_factory);

        var keys = Directory.GetFiles(_factory.KeysFolder, "key-*.xml");
        Assert.NotEmpty(keys);
        Assert.Contains("<key", await File.ReadAllTextAsync(keys[0]));
    }
}

public sealed class AdminLoginStartupTests
{
    [Fact]
    public void Enabling_the_login_without_a_password_stops_the_application()
    {
        using var factory = new WebAppFactory().WithWebHostBuilder(b => b.UseSetting("Admin:Enabled", "true"));

        var failure = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Admin:PasswordHash", Flatten(failure));
    }

    [Fact]
    public void A_password_hash_that_is_not_a_hash_stops_the_application()
    {
        using var factory = new WebAppFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("Admin:Enabled", "true");
            b.UseSetting("Admin:PasswordHash", "not-a-hash!");
        });

        var failure = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("not a valid", Flatten(failure));
    }

    [Fact]
    public async Task With_the_login_disabled_everything_stays_open()
    {
        using var factory = new WebAppFactory();
        var client = factory.CreateBrowser();

        foreach (var path in new[] { "/", "/partners", "/messages", "/sender" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        }

        var html = await client.GetStringAsync("/partners");
        Assert.DoesNotContain("account/logout", html);
        Assert.DoesNotContain("nav-username", html);
        // The login page explains that there is nothing to sign in to; a login post just goes to the dashboard.
        Assert.Contains("switched off", await client.GetStringAsync("/login"));
        var post = await client.PostAsync("/account/login", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal("/", post.Headers.Location!.OriginalString);
    }

    private static string Flatten(Exception ex)
    {
        var text = new StringBuilder();
        for (var e = ex; e is not null; e = e.InnerException!)
        {
            text.AppendLine(e.Message);
            if (e.InnerException is null)
            {
                break;
            }
        }

        return text.ToString();
    }
}
