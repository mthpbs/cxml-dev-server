using System.Net;
using System.Text.RegularExpressions;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Web.Tests;

public sealed partial class OrderAndScenarioTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public OrderAndScenarioTests(WebAppFactory factory) => _factory = factory;

    [GeneratedRegex("orderID=\"([^\"]+)\"")]
    private static partial Regex OrderIdRegex();

    [Fact]
    public async Task The_specification_order_request_example_is_accepted_and_persisted()
    {
        var xml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Samples", "0-OrderRequest.xml"));

        var response = await _factory.CreateClient().PostCxmlAsync(xml);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(_factory.Validate(body).IsValid);
        Assert.Equal(200, _factory.Read(body).Response?.Status.Code);

        var order = await _factory.QueryAsync(db => db.PurchaseOrders.AsNoTracking().Include(o => o.Lines).SingleAsync(o => o.OrderId == "DO1234"));
        Assert.Equal(3, order.Lines.Count);
        Assert.Equal(187.60m, order.TotalAmount);

        // The order and its message are visible in the UI.
        var page = await _factory.CreateClient().GetStringAsync($"/orders/{order.Id}");
        Assert.Contains("DO1234", page);
        Assert.Contains("hello from item 1", page);
    }

    [Fact]
    public async Task A_scenario_forces_cxml_500_once_and_then_the_order_is_accepted()
    {
        var order = await _factory.RenderTemplateAsync("order-request");
        var orderId = OrderIdRegex().Match(order).Groups[1].Value;
        await _factory.QueryAsync(async db =>
        {
            db.ResponseScenarios.Add(new ResponseScenario
            {
                Name = "web test: fail once", DocumentType = "OrderRequest", MatchContains = orderId,
                StatusCode = 500, StatusDetail = "forced", RemainingUses = 1, Priority = 100,
            });
            await db.SaveChangesAsync();
            return 0;
        });
        var client = _factory.CreateClient();

        var first = await client.PostCxmlAsync(order);
        var second = await client.PostCxmlAsync(order);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(500, _factory.Read(await first.Content.ReadAsStringAsync()).Response?.Status.Code);
        Assert.Equal(200, _factory.Read(await second.Content.ReadAsStringAsync()).Response?.Status.Code);
        Assert.Single(await _factory.QueryAsync(db => db.PurchaseOrders.Where(o => o.OrderId == orderId).ToListAsync()));
    }

    [Fact]
    public async Task A_scenario_can_change_the_http_status()
    {
        var request = (await _factory.RenderTemplateAsync("profile-request")).Replace("<Header>", "<!-- http-503-marker --><Header>");
        await _factory.QueryAsync(async db =>
        {
            db.ResponseScenarios.Add(new ResponseScenario
            {
                Name = "web test: service unavailable", DocumentType = "ProfileRequest", MatchContains = "http-503-marker",
                StatusCode = 503, HttpStatus = 503, Priority = 100,
            });
            await db.SaveChangesAsync();
            return 0;
        });

        var response = await _factory.CreateClient().PostCxmlAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(503, _factory.Read(await response.Content.ReadAsStringAsync()).Response?.Status.Code);
    }

    [Fact]
    public async Task A_no_response_scenario_drops_the_connection()
    {
        var request = (await _factory.RenderTemplateAsync("profile-request")).Replace("<Header>", "<!-- abort-marker --><Header>");
        await _factory.QueryAsync(async db =>
        {
            db.ResponseScenarios.Add(new ResponseScenario
            {
                Name = "web test: timeout", DocumentType = "ProfileRequest", MatchContains = "abort-marker",
                Behavior = ScenarioBehavior.NoResponse, Priority = 100,
            });
            await db.SaveChangesAsync();
            return 0;
        });

        await Assert.ThrowsAnyAsync<Exception>(() => _factory.CreateClient().PostCxmlAsync(request));
    }
}
