using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Tests;

public class ScenarioTests
{
    /// <summary>The scenarios created by a test (the seeded sample is excluded).</summary>
    private static IQueryable<ResponseScenario> Mine(CxmlDevServer.Core.Abstractions.ICxmlDbContext db) =>
        db.ResponseScenarios.Where(s => !s.Name.StartsWith("Sample:"));

    private static Task AddAsync(TestApp app, params ResponseScenario[] scenarios) =>
        app.ModifyAsync(db =>
        {
            db.ResponseScenarios.AddRange(scenarios);
            return Task.CompletedTask;
        });

    [Fact]
    public async Task Limited_scenario_fails_once_and_then_stops_matching()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario
        {
            Name = "fail once", DocumentType = "OrderRequest", StatusCode = 500, StatusDetail = "boom", RemainingUses = 1,
        });

        var first = await app.PostAsync(Docs.OrderRequest("PO-1"));
        var status = TestApp.StatusOf(first);
        Assert.Equal(500, status.Code);
        Assert.Equal("boom", status.Detail);
        Assert.Equal(200, first.HttpStatus);
        Assert.Empty(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));

        var second = await app.PostAsync(Docs.OrderRequest("PO-1"));
        Assert.Equal(200, TestApp.StatusOf(second).Code);
        Assert.Single(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));

        var scenario = await app.QueryAsync(db => Mine(db).SingleAsync());
        Assert.Equal(0, scenario.RemainingUses);
        Assert.Equal(1, scenario.HitCount);
        Assert.NotNull(scenario.LastHitAt);

        var log = await app.GetLogAsync(first.LogId!.Value);
        Assert.Equal(scenario.Id, log.ScenarioId);
        Assert.Contains("fail once", log.Notes);
        Assert.Equal(500, log.CxmlStatusCode);
    }

    [Fact]
    public async Task Unlimited_scenarios_keep_firing_and_count_hits()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario { Name = "always", StatusCode = 560 });

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(560, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest())).Code);
        }

        var scenario = await app.QueryAsync(db => Mine(db).SingleAsync());
        Assert.Null(scenario.RemainingUses);
        Assert.Equal(3, scenario.HitCount);
    }

    [Fact]
    public async Task Default_status_of_a_scenario_is_500_and_text_can_be_customised()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario { Name = "defaults" });
        var status = TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest()));
        Assert.Equal(500, status.Code);
        Assert.Equal("Internal Server Error", status.Text);

        await app.ModifyAsync(async db =>
        {
            var s = await Mine(db).SingleAsync();
            s.StatusCode = 409;
            s.StatusText = "Duplicate";
        });
        status = TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest()));
        Assert.Equal(409, status.Code);
        Assert.Equal("Duplicate", status.Text);
    }

    [Fact]
    public async Task Scenarios_match_on_content_document_type_partner_and_enabled_flag()
    {
        await using var app = await TestApp.CreateAsync();
        var acme = (await app.GetPartnerAsync("Acme (spec example)")).Id;
        var demo = (await app.GetPartnerAsync("Demo Buyer")).Id;
        await AddAsync(app,
            new ResponseScenario { Name = "content", MatchContains = "PO-MAGIC", StatusCode = 412 },
            new ResponseScenario { Name = "wrong type", DocumentType = "PunchOutSetupRequest", StatusCode = 413 },
            new ResponseScenario { Name = "other partner", PartnerId = acme, StatusCode = 414 },
            new ResponseScenario { Name = "disabled", Enabled = false, StatusCode = 415 });

        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-PLAIN"))).Code);
        Assert.Equal(412, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-MAGIC"))).Code);

        await app.ModifyAsync(async db => (await db.ResponseScenarios.SingleAsync(s => s.Name == "other partner")).PartnerId = demo);
        Assert.Equal(414, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-PLAIN"))).Code);
    }

    [Fact]
    public async Task Highest_priority_scenario_wins()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app,
            new ResponseScenario { Name = "low", Priority = 1, StatusCode = 501 },
            new ResponseScenario { Name = "high", Priority = 9, StatusCode = 502, RemainingUses = 1 });

        Assert.Equal(502, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest())).Code);
        // Once the high-priority scenario is used up the next one applies.
        Assert.Equal(501, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest())).Code);
    }

    [Fact]
    public async Task Scenarios_do_not_apply_to_unauthenticated_requests()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario { Name = "always", StatusCode = 500 });

        Assert.Equal(401, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest().Replace("devsecret", "x"))).Code);
        Assert.Equal(0, (await app.QueryAsync(db => Mine(db).SingleAsync())).HitCount);
    }

    [Fact]
    public async Task Delay_is_applied_and_reported()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario { Name = "slow", Behavior = ScenarioBehavior.Passthrough, DelayMs = 120 });

        var result = await app.PostAsync(Docs.OrderRequest("PO-SLOW"));

        Assert.Equal(TimeSpan.FromMilliseconds(120), result.DelayApplied);
        Assert.Equal(200, TestApp.StatusOf(result).Code);
        Assert.Single(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync())); // passthrough still processes the order
        Assert.True((await app.GetLogAsync(result.LogId!.Value)).DurationMs >= 100);
    }

    [Fact]
    public async Task Scenario_can_override_the_http_status()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario { Name = "http 503", StatusCode = 560, HttpStatus = 503 });

        var result = await app.PostAsync(Docs.OrderRequest());

        Assert.Equal(503, result.HttpStatus);
        Assert.Equal(560, TestApp.StatusOf(result).Code);
        Assert.Equal(503, (await app.GetLogAsync(result.LogId!.Value)).HttpStatus);
    }

    [Fact]
    public async Task Malformed_body_scenario_returns_a_broken_document()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario { Name = "broken", Behavior = ScenarioBehavior.MalformedBody });

        var result = await app.PostAsync(Docs.OrderRequest(), validateResponse: false);

        Assert.StartsWith("<?xml", result.ResponseText);
        Assert.Throws<Cxml.Protocol.Parsing.CxmlParseException>(() => TestApp.Parse(result.ResponseText));
        Assert.Empty(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));

        await app.ModifyAsync(async db => (await Mine(db).SingleAsync()).MalformedBody = "<html>Bad Gateway</html>");
        var custom = await app.PostAsync(Docs.OrderRequest(), validateResponse: false);
        Assert.Equal("<html>Bad Gateway</html>", custom.ResponseText);
    }

    [Fact]
    public async Task No_response_scenario_aborts_the_connection_after_the_delay()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario { Name = "timeout", Behavior = ScenarioBehavior.NoResponse, DelayMs = 50 });

        var result = await app.PostAsync(Docs.OrderRequest(), validateResponse: false);

        Assert.True(result.Abort);
        Assert.Empty(result.ResponseBytes);
        Assert.Equal(TimeSpan.FromMilliseconds(50), result.DelayApplied);
        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Null(log.HttpStatus);
        Assert.Null(log.ResponseXml);
        Assert.NotNull(log.ScenarioId);
    }

    [Fact]
    public async Task Cancellation_during_a_scenario_delay_propagates()
    {
        await using var app = await TestApp.CreateAsync();
        await AddAsync(app, new ResponseScenario { Name = "slow", Behavior = ScenarioBehavior.Passthrough, DelayMs = 60_000 });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => app.ScopeAsync(sp =>
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<CxmlDevServer.Core.Pipeline.CxmlInboundProcessor>(sp)
                .ProcessAsync(new CxmlDevServer.Core.Pipeline.CxmlInboundRequest
                {
                    Body = System.Text.Encoding.UTF8.GetBytes(Docs.OrderRequest()),
                    BaseUrl = TestApp.BaseUrl,
                }, cts.Token)));
    }

    [Fact]
    public async Task Concurrent_requests_cannot_use_a_single_use_scenario_twice()
    {
        await using var app = await TestApp.CreateAsync(fileDatabase: true);
        await AddAsync(app, new ResponseScenario { Name = "once", StatusCode = 500, RemainingUses = 1 });

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(i => app.PostAsync(Docs.OrderRequest("PO-C" + i))));

        Assert.Equal(1, results.Count(r => TestApp.StatusOf(r).Code == 500));
        Assert.Equal(3, results.Count(r => TestApp.StatusOf(r).Code == 200));
    }
}
