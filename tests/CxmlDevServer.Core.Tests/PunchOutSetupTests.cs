using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Handlers;
using CxmlDevServer.Core.PunchOut;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Tests;

public class PunchOutSetupTests
{
    private static string StartPageUrl(string responseText) =>
        ((PunchOutSetupResponse)TestApp.Parse(responseText).Response!.Payload!).StartPageUrl;

    private static Task<PunchOutSession> LoadSessionAsync(TestApp app, string token) =>
        app.QueryAsync(db => db.PunchOutSessions.AsNoTracking().Include(s => s.Lines).Include(s => s.Partner).SingleAsync(s => s.Token == token));

    [Fact]
    public async Task Create_returns_a_start_page_url_and_persists_the_session()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.PunchOutSetup(payloadId: "setup-1@buyer", buyerCookie: "cookie-abc");

        var result = await app.PostAsync(xml);

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var url = StartPageUrl(result.ResponseText);
        Assert.StartsWith(TestApp.BaseUrl + "/punchout/", url);
        var token = url[(TestApp.BaseUrl + "/punchout/").Length..];
        Assert.Equal(43, token.Length); // 32 random bytes, URL-safe base64 without padding
        Assert.Matches("^[A-Za-z0-9_-]+$", token);

        var session = await LoadSessionAsync(app, token);
        Assert.Equal(PunchOutOperation.Create, session.Operation);
        Assert.Equal(PunchOutSessionStatus.Created, session.Status);
        Assert.Equal("cookie-abc", session.BuyerCookie);
        Assert.Equal("setup-1@buyer", session.SetupPayloadId);
        Assert.Equal("https://buyer.example/return", session.BrowserFormPostUrl);
        Assert.Equal("Demo Buyer", session.Partner!.Name);
        Assert.Equal("NetworkId", session.BuyerDomain);
        Assert.Equal("AN01000000001", session.BuyerIdentity);
        Assert.Equal("test", session.DeploymentMode);
        Assert.Contains("UserEmail", session.ExtrinsicsJson);
        Assert.Contains("John Doe", session.ContactsJson);
        Assert.True(session.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(59));
        Assert.True(session.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(61));
        Assert.Empty(session.Lines);

        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Equal("PunchOutSetupRequest", log.DocumentType);
        Assert.Equal(session.CorrelationId, log.CorrelationId);
        Assert.Equal(log.Id, session.SetupMessageLogId);
    }

    [Fact]
    public async Task Public_base_url_overrides_the_request_url()
    {
        await using var app = await TestApp.CreateAsync(o => o.PublicBaseUrl = "https://dev.example.com/");

        var result = await app.PostAsync(Docs.PunchOutSetup());

        Assert.StartsWith("https://dev.example.com/punchout/", StartPageUrl(result.ResponseText));
    }

    [Fact]
    public async Task Each_setup_gets_a_unique_token_and_the_session_lifetime_is_configurable()
    {
        await using var app = await TestApp.CreateAsync(o => o.PunchOutSessionMinutes = 5);

        var first = StartPageUrl((await app.PostAsync(Docs.PunchOutSetup())).ResponseText);
        var second = StartPageUrl((await app.PostAsync(Docs.PunchOutSetup())).ResponseText);

        Assert.NotEqual(first, second);
        var sessions = await app.QueryAsync(db => db.PunchOutSessions.ToListAsync());
        Assert.All(sessions, s => Assert.True(s.ExpiresAt < DateTimeOffset.UtcNow.AddMinutes(6)));
    }

    [Fact]
    public async Task Buyer_cookie_with_markup_is_stored_verbatim()
    {
        await using var app = await TestApp.CreateAsync();
        const string cookie = "<Extrinsic name=\"session\">7</Extrinsic>";

        var result = await app.PostAsync(Docs.PunchOutSetup(buyerCookie: cookie));

        var token = StartPageUrl(result.ResponseText).Split('/').Last();
        Assert.Equal(cookie, (await LoadSessionAsync(app, token)).BuyerCookie);
    }

    [Fact]
    public async Task Source_operation_is_not_implemented()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(Docs.PunchOutSetup("source"));

        var status = TestApp.StatusOf(result);
        Assert.Equal(450, status.Code);
        Assert.Contains("source", status.Detail);
        Assert.Empty(await app.QueryAsync(db => db.PunchOutSessions.ToListAsync()));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("edit")]
    public async Task Missing_browser_form_post_is_a_bad_request_for_create_and_edit(string operation)
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(Docs.PunchOutSetup(operation, browserFormPost: null, itemOutXml: Docs.ItemOut("X", 1, price: 1m)));

        var status = TestApp.StatusOf(result);
        Assert.Equal(400, status.Code);
        Assert.Contains("BrowserFormPost", status.Detail);
    }

    [Fact]
    public async Task Relative_browser_form_post_is_a_bad_request()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(Docs.PunchOutSetup(browserFormPost: "/return"));

        Assert.Equal(400, TestApp.StatusOf(result).Code);
    }

    [Fact]
    public async Task Inspect_does_not_need_a_browser_form_post()
    {
        await using var app = await TestApp.CreateAsync();
        var item = Docs.ItemOut("LAP-1001", 1, price: 899m);

        var result = await app.PostAsync(Docs.PunchOutSetup("inspect", browserFormPost: null, itemOutXml: item));

        Assert.Equal(200, TestApp.StatusOf(result).Code);
    }

    [Fact]
    public async Task Empty_buyer_cookie_is_a_bad_request()
    {
        await using var app = await TestApp.CreateAsync();

        Assert.Equal(400, TestApp.StatusOf(await app.PostAsync(Docs.PunchOutSetup(buyerCookie: " "))).Code);
    }

    [Fact]
    public async Task Edit_rebuilds_the_cart_from_cookie_part_number_and_ad_hoc_items()
    {
        await using var app = await TestApp.CreateAsync();
        var items = await app.QueryAsync(db => db.CatalogItems.AsNoTracking().OrderBy(i => i.Id).Take(2).ToListAsync());
        var byCookie = items[0];
        var byPartId = items[1];
        var xml = Docs.PunchOutSetup(
            "edit",
            itemOutXml: string.Concat(
                // matched by the cookie even though the part number is wrong
                Docs.ItemOut("WRONG-PART", 2, aux: PunchOutCookies.ForCatalogItem(byCookie.Id), price: 0.01m),
                // matched by part number, no cookie
                Docs.ItemOut(byPartId.SupplierPartId, 3),
                // not in the catalog: ad hoc, copied from ItemDetail
                Docs.ItemOut("ADHOC-1", 4, aux: "<Extrinsic name=\"ref\">7</Extrinsic>", price: 12.5m, name: "Custom cable", unspsc: "26121609")));

        var result = await app.PostAsync(xml);

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var token = StartPageUrl(result.ResponseText).Split('/').Last();
        var session = await LoadSessionAsync(app, token);
        var lines = session.Lines.OrderBy(l => l.LineNumber).ToList();
        Assert.Equal(3, lines.Count);

        Assert.Equal(byCookie.Id, lines[0].CatalogItemId);
        Assert.Equal(byCookie.SupplierPartId, lines[0].SupplierPartId);
        Assert.Equal(byCookie.UnitPrice, lines[0].UnitPrice); // the catalog is authoritative for price
        Assert.Equal(2, lines[0].Quantity);
        Assert.False(lines[0].IsAdHoc);

        Assert.Equal(byPartId.Id, lines[1].CatalogItemId);
        Assert.Equal(3, lines[1].Quantity);
        Assert.Equal(PunchOutCookies.ForCatalogItem(byPartId.Id), lines[1].SupplierPartAuxiliaryId);

        Assert.Null(lines[2].CatalogItemId);
        Assert.True(lines[2].IsAdHoc);
        Assert.Equal("ADHOC-1", lines[2].SupplierPartId);
        Assert.Equal("<Extrinsic name=\"ref\">7</Extrinsic>", lines[2].SupplierPartAuxiliaryId);
        Assert.Equal(12.5m, lines[2].UnitPrice);
        Assert.Equal("Custom cable", lines[2].Description);
        Assert.Equal("26121609", lines[2].Unspsc);
        Assert.Equal(4, lines[2].Quantity);

        Assert.NotNull(session.OriginalLinesJson);
    }

    [Fact]
    public async Task Edit_and_inspect_without_items_fail_in_strict_mode_and_warn_in_lenient_mode()
    {
        await using var app = await TestApp.CreateAsync();

        var lenient = await app.PostAsync(Docs.PunchOutSetup("edit"));
        Assert.Equal(200, TestApp.StatusOf(lenient).Code);
        Assert.Contains((await app.GetLogAsync(lenient.LogId!.Value)).Issues,
            i => i.Code == PunchOutSetupHandler.NoItemsCode && i.Severity == ValidationSeverity.Warning);

        await app.SetPartnerAsync("Demo Buyer", p => p.ValidationMode = ValidationMode.Strict);
        Assert.Equal(412, TestApp.StatusOf(await app.PostAsync(Docs.PunchOutSetup("edit"))).Code);
        Assert.Equal(412, TestApp.StatusOf(await app.PostAsync(Docs.PunchOutSetup("inspect", browserFormPost: null))).Code);
        Assert.Equal(1, await app.QueryAsync(db => db.PunchOutSessions.CountAsync()));
    }

    [Fact]
    public async Task Setup_with_wrong_secret_creates_no_session()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(Docs.PunchOutSetup(secret: "nope"));

        Assert.Equal(401, TestApp.StatusOf(result).Code);
        Assert.Empty(await app.QueryAsync(db => db.PunchOutSessions.ToListAsync()));
    }
}
