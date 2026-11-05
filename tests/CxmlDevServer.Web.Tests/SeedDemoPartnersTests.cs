using System.Net;
using CxmlDevServer.Web.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Web.Tests;

public sealed class SeedDemoPartnersTests
{
    [Fact]
    public async Task Without_demo_partners_the_app_starts_with_no_partners_and_the_pages_still_render()
    {
        using var factory = new NoDemoPartnersFactory();
        var client = factory.CreateClient();

        foreach (var path in new[] { "/", "/partners", "/sender", "/scenarios", "/identities", "/catalog" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        }

        var html = await client.GetStringAsync("/");
        Assert.DoesNotContain("devsecret", html);
        Assert.DoesNotContain("abracadabra", html);
        Assert.Equal(0, await factory.QueryAsync(db => db.TradingPartners.CountAsync()));
        Assert.Equal(0, await factory.QueryAsync(db => db.ResponseScenarios.CountAsync()));
        Assert.Equal(2, await factory.QueryAsync(db => db.SupplierIdentities.CountAsync()));
        Assert.True(await factory.QueryAsync(db => db.CatalogItems.CountAsync()) > 0);
    }

    [Fact]
    public async Task A_sender_is_unauthorized_when_there_are_no_partners()
    {
        using var factory = new NoDemoPartnersFactory();
        var xml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Samples", "0-OrderRequest.xml"));

        var response = await factory.CreateClient().PostCxmlAsync(xml);

        Assert.Contains("code=\"401\"", await response.Content.ReadAsStringAsync());
    }

    private sealed class NoDemoPartnersFactory : WebAppFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Cxml:SeedDemoPartners", "false");
        }
    }
}
