using System.Net;
using CxmlDevServer.Web.Tests.Support;

namespace CxmlDevServer.Web.Tests;

public sealed class PageSmokeTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public PageSmokeTests(WebAppFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/partners")]
    [InlineData("/partners/new")]
    [InlineData("/partners/1")]
    [InlineData("/identities")]
    [InlineData("/catalog")]
    [InlineData("/catalog/new")]
    [InlineData("/catalog/1")]
    [InlineData("/messages")]
    [InlineData("/orders")]
    [InlineData("/sessions")]
    [InlineData("/scenarios")]
    [InlineData("/sender")]
    [InlineData("/rules")]
    public async Task Admin_pages_render(string path)
    {
        var response = await _factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("cXML Dev Server", html);
    }

    [Fact]
    public async Task Dashboard_shows_the_endpoint_and_the_demo_credentials()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");

        Assert.Contains("http://localhost/cxml", html);
        Assert.Contains("devsecret", html);
        Assert.Contains("123456789", html);
    }

    [Fact]
    public async Task Sender_preloads_a_template_for_the_demo_partner()
    {
        var html = await _factory.CreateClient().GetStringAsync("/sender?template=punchout-setup-create");

        Assert.Contains("PunchOutSetupRequest", html);
        Assert.Contains("http://localhost/test-buyer/punchout-return", html);
    }
}
