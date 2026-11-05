using System.Net;
using System.Text;
using Cxml.Protocol.Documents.PunchOut;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.PunchOut;
using CxmlDevServer.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Web.Tests;

public sealed class PunchOutRoundTripTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public PunchOutRoundTripTests(WebAppFactory factory) => _factory = factory;

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string url, params (string Name, string Value)[] fields) =>
        await client.PostAsync(url, new FormUrlEncodedContent(fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value))));

    /// <summary>Sets up a session through POST /cxml and returns its token and the BuyerCookie.</summary>
    private async Task<(HttpClient Browser, string Token, string StartPage, string BuyerCookie)> StartSessionAsync()
    {
        var setup = await _factory.RenderTemplateAsync("punchout-setup-create");
        var browser = _factory.CreateBrowser();

        var response = await browser.PostCxmlAsync(setup);
        var xml = await response.Content.ReadAsStringAsync();
        Assert.True(_factory.Validate(xml).IsValid, "The PunchOutSetupResponse is not DTD valid.");
        var envelope = _factory.Read(xml);
        Assert.Equal(200, envelope.Response?.Status.Code);
        var startPage = Assert.IsType<PunchOutSetupResponse>(envelope.Response!.Payload).StartPageUrl;
        Assert.StartsWith("http://localhost/punchout/", startPage);

        var cookie = _factory.Read(setup).Request!.Payload is PunchOutSetupRequest request ? request.BuyerCookie : throw new InvalidOperationException();
        return (browser, startPage[(startPage.LastIndexOf('/') + 1)..], startPage, cookie);
    }

    [Fact]
    public async Task Full_round_trip_from_setup_to_the_test_buyer()
    {
        var (browser, token, startPage, buyerCookie) = await StartSessionAsync();

        // The StartPage is a normal HTML page with the shop and the forms that leave it.
        var shop = await browser.GetAsync(startPage);
        Assert.Equal(HttpStatusCode.OK, shop.StatusCode);
        Assert.StartsWith("text/html", shop.Content.Headers.ContentType?.ToString());
        var shopHtml = await shop.Content.ReadAsStringAsync();
        Assert.Contains("Dev Supplier Catalog", shopHtml);
        Assert.Contains($"action=\"punchout/{token}/checkout\"", shopHtml);
        var antiforgery = Http.ExtractAntiforgeryToken(shopHtml);

        // The user fills the cart (the same service the shop page calls).
        await _factory.WithScopeAsync(async sp =>
        {
            var service = sp.GetRequiredService<PunchOutService>();
            await service.AddToCartAsync(token, 1, 2);
            await service.AddToCartAsync(token, 6, 1);
            return 0;
        });

        // Return to the procurement system: an HTML page with a form that posts to the BrowserFormPost URL.
        var checkout = await PostFormAsync(browser, $"/punchout/{token}/checkout", ("__RequestVerificationToken", antiforgery));
        Assert.Equal(HttpStatusCode.OK, checkout.StatusCode);
        var checkoutHtml = await checkout.Content.ReadAsStringAsync();
        Assert.Contains("<button type=\"submit\"", checkoutHtml);
        Assert.Contains("document.getElementById('punchout-return').submit()", checkoutHtml);
        Assert.Contains("<details", checkoutHtml);
        var (action, field, value) = Http.ParseReturnForm(checkoutHtml);
        Assert.Equal("http://localhost/test-buyer/punchout-return", action);
        Assert.Equal("cXML-base64", field);

        // The field decodes to a valid PunchOutOrderMessage that echoes the BuyerCookie.
        var orderXml = Encoding.UTF8.GetString(Convert.FromBase64String(value));
        Assert.True(_factory.Validate(orderXml).IsValid, "The PunchOutOrderMessage is not DTD valid.");
        var message = Assert.IsType<PunchOutOrderMessage>(_factory.Read(orderXml).Message!.Payload);
        Assert.Equal(buyerCookie, message.BuyerCookie);
        Assert.Equal(2, message.Items.Count);

        // The browser posts the form to the built-in test buyer, which accepts and logs it.
        var received = await PostFormAsync(_factory.CreateClient(), "/test-buyer/punchout-return", (field, value));
        Assert.Equal(HttpStatusCode.OK, received.StatusCode);
        var receivedHtml = await received.Content.ReadAsStringAsync();
        Assert.Contains("Valid PunchOutOrderMessage", receivedHtml);
        Assert.Contains(buyerCookie, receivedHtml);

        var logged = await _factory.QueryAsync(db => db.MessageLogs.AsNoTracking()
            .Where(m => m.Channel == MessageChannel.BrowserPost).OrderBy(m => m.Id).ToListAsync());
        Assert.Contains(logged, m => m.Direction == MessageDirection.Outbound && m.DocumentType == "PunchOutOrderMessage");
        Assert.Contains(logged, m => m.Direction == MessageDirection.Inbound && m.DocumentType == "PunchOutOrderMessage" && m.RawXml == orderXml);
        var session = await _factory.QueryAsync(db => db.PunchOutSessions.AsNoTracking().SingleAsync(s => s.Token == token));
        Assert.Equal(PunchOutSessionStatus.Submitted, session.Status);
    }

    [Fact]
    public async Task Checkout_cannot_be_submitted_twice()
    {
        var (browser, token, startPage, _) = await StartSessionAsync();
        var antiforgery = Http.ExtractAntiforgeryToken(await browser.GetStringAsync(startPage));
        await _factory.WithScopeAsync(async sp =>
        {
            await sp.GetRequiredService<PunchOutService>().AddToCartAsync(token, 1);
            return 0;
        });

        var first = await PostFormAsync(browser, $"/punchout/{token}/checkout", ("__RequestVerificationToken", antiforgery));
        var second = await PostFormAsync(browser, $"/punchout/{token}/checkout", ("__RequestVerificationToken", antiforgery));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("already been completed", await second.Content.ReadAsStringAsync());
        var returns = await _factory.QueryAsync(db => db.MessageLogs.CountAsync(m =>
            m.Direction == MessageDirection.Outbound && m.Channel == MessageChannel.BrowserPost && m.CorrelationId ==
            db.PunchOutSessions.Where(s => s.Token == token).Select(s => s.CorrelationId).First()));
        Assert.Equal(1, returns);
    }

    [Fact]
    public async Task Checkout_without_an_antiforgery_token_is_refused()
    {
        var (browser, token, _, _) = await StartSessionAsync();

        var response = await PostFormAsync(browser, $"/punchout/{token}/checkout");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var session = await _factory.QueryAsync(db => db.PunchOutSessions.AsNoTracking().SingleAsync(s => s.Token == token));
        Assert.NotEqual(PunchOutSessionStatus.Submitted, session.Status);
    }

    [Fact]
    public async Task Cancel_returns_an_empty_cart_and_ends_the_session()
    {
        var (browser, token, startPage, _) = await StartSessionAsync();
        var antiforgery = Http.ExtractAntiforgeryToken(await browser.GetStringAsync(startPage));

        var cancel = await PostFormAsync(browser, $"/punchout/{token}/cancel", ("__RequestVerificationToken", antiforgery));

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var (_, _, value) = Http.ParseReturnForm(await cancel.Content.ReadAsStringAsync());
        var orderXml = Encoding.UTF8.GetString(Convert.FromBase64String(value));
        Assert.True(_factory.Validate(orderXml).IsValid);
        Assert.Empty(Assert.IsType<PunchOutOrderMessage>(_factory.Read(orderXml).Message!.Payload).Items);

        // The finished session shows a friendly page instead of the shop.
        var again = await browser.GetStringAsync(startPage);
        Assert.Contains("This PunchOut session is finished", again);
    }

    [Theory]
    [InlineData("does-not-exist", "does not exist")]
    public async Task Unknown_sessions_show_a_friendly_page(string token, string expected)
    {
        var response = await _factory.CreateClient().GetAsync($"/punchout/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Test_buyer_reports_a_post_without_a_cxml_field()
    {
        var response = await PostFormAsync(_factory.CreateClient(), "/test-buyer/punchout-return", ("something", "else"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("could not be processed", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Urlencoded_partners_get_the_xml_html_encoded_in_the_hidden_field()
    {
        // The "Acme" partner returns carts as cXML-urlencoded; the value must reach the buyer decodable.
        var setup = (await _factory.RenderTemplateAsync("punchout-setup-create"))
            .Replace("NetworkId", "AribaNetworkUserId")
            .Replace("AN01000000001", "admin@acme.com")
            .Replace("devsecret", "abracadabra");
        var browser = _factory.CreateBrowser();
        var envelope = _factory.Read(await (await browser.PostCxmlAsync(setup)).Content.ReadAsStringAsync());
        Assert.Equal(200, envelope.Response?.Status.Code);
        var startPage = ((PunchOutSetupResponse)envelope.Response!.Payload!).StartPageUrl;
        var token = startPage[(startPage.LastIndexOf('/') + 1)..];
        var antiforgery = Http.ExtractAntiforgeryToken(await browser.GetStringAsync(startPage));
        await _factory.WithScopeAsync(async sp =>
        {
            await sp.GetRequiredService<PunchOutService>().AddToCartAsync(token, 2);
            return 0;
        });

        var checkout = await PostFormAsync(browser, $"/punchout/{token}/checkout", ("__RequestVerificationToken", antiforgery));

        var html = await checkout.Content.ReadAsStringAsync();
        var (_, field, value) = Http.ParseReturnForm(html);
        Assert.Equal("cXML-urlencoded", field);
        // What the browser submits (the attribute value after HTML decoding) is the document itself.
        Assert.StartsWith("<?xml", value);
        Assert.True(_factory.Validate(value).IsValid, "The returned document is not DTD valid.");
        var received = await PostFormAsync(_factory.CreateClient(), "/test-buyer/punchout-return", (field, value));
        Assert.Contains("Valid PunchOutOrderMessage", await received.Content.ReadAsStringAsync());
    }
}
