using Cxml.Protocol.Documents.PunchOut;
using CxmlDevServer.Web.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CxmlDevServer.Web.Tests;

/// <summary>
/// Behind a TLS-terminating proxy (Azure Container Apps ingress, Caddy) the StartPage URL must come out as https.
/// The default host enables the forwarded headers middleware when <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED=true</c>,
/// which is the configuration key <c>FORWARDEDHEADERS_ENABLED</c> of the (ASPNETCORE_ prefixed) host configuration.
/// </summary>
public sealed class ForwardedHeadersTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public ForwardedHeadersTests(WebAppFactory factory) => _factory = factory;

    private static async Task<string> StartPageViaProxyAsync(WebApplicationFactory<Program> factory, string setup)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/cxml")
        {
            Content = new StringContent(setup, System.Text.Encoding.UTF8, "text/xml"),
        };
        request.Headers.Host = "dev.example.com";
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");
        var response = await client.SendAsync(request);
        var envelope = factory.Services.GetService(typeof(Cxml.Protocol.Parsing.CxmlReader)) as Cxml.Protocol.Parsing.CxmlReader;
        var parsed = envelope!.Read(await response.Content.ReadAsStringAsync());
        return Assert.IsType<PunchOutSetupResponse>(parsed.Response!.Payload).StartPageUrl;
    }

    [Fact]
    public async Task Without_forwarded_headers_enabled_the_proxy_headers_are_ignored()
    {
        var setup = await _factory.RenderTemplateAsync("punchout-setup-create");

        var startPage = await StartPageViaProxyAsync(_factory, setup);

        Assert.StartsWith("http://dev.example.com/punchout/", startPage);
    }

    [Fact]
    public async Task With_forwarded_headers_enabled_the_start_page_is_https()
    {
        var setup = await _factory.RenderTemplateAsync("punchout-setup-create");
        using var proxied = _factory.WithWebHostBuilder(b => b.UseSetting("FORWARDEDHEADERS_ENABLED", "true"));

        var startPage = await StartPageViaProxyAsync(proxied, setup);

        Assert.StartsWith("https://dev.example.com/punchout/", startPage);
    }

    [Fact]
    public async Task A_configured_public_base_url_still_wins_behind_a_proxy()
    {
        var setup = await _factory.RenderTemplateAsync("punchout-setup-create");
        using var proxied = _factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("FORWARDEDHEADERS_ENABLED", "true");
            b.UseSetting("Cxml:PublicBaseUrl", "https://supplier.example.org");
        });

        var startPage = await StartPageViaProxyAsync(proxied, setup);

        Assert.StartsWith("https://supplier.example.org/punchout/", startPage);
    }
}
