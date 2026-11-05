using Cxml.Protocol;
using Cxml.Protocol.Documents;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.DependencyInjection;
using CxmlDevServer.Core.Handlers;
using CxmlDevServer.Core.Pipeline;
using CxmlDevServer.Core.Rules;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Tests;

public class ExtensibilityTests
{
    private sealed class ThrowingHandler : ICxmlRequestHandler
    {
        public string RequestName => "ExplodeRequest";

        public Task<CxmlHandlerResult> HandleAsync(CxmlInboundContext context) =>
            throw new InvalidOperationException("kaboom");
    }

    private sealed class PingPayload : ICxmlPayload
    {
        public string ElementName => "PingRequest";
        public string Text { get; init; } = string.Empty;
    }

    private sealed class PingCodec : ICxmlPayloadParser, ICxmlPayloadWriter
    {
        public string ElementName => "PingRequest";
        public ICxmlPayload Parse(System.Xml.Linq.XElement element) => new PingPayload { Text = element.Value };
        public System.Xml.Linq.XElement Write(ICxmlPayload payload) => new("PingRequest", ((PingPayload)payload).Text);
    }

    private sealed class PingHandler : ICxmlRequestHandler
    {
        public string RequestName => "PingRequest";

        public Task<CxmlHandlerResult> HandleAsync(CxmlInboundContext context) =>
            Task.FromResult(CxmlHandlerResult.Ok(detail: "pong " + ((PingPayload)context.Envelope!.Request!.Payload!).Text));
    }

    private sealed class NoPingsRule : DocumentRuleBase
    {
        public override string Code => "RULE-TEST-NO-PINGS";
        public override string Description => "Test rule.";
        public override bool AppliesTo(CxmlRuleContext context) => PayloadOf<PingPayload>(context) is not null;

        public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
        {
            yield return Warning("pings are discouraged");
        }
    }

    private sealed class TaggingStep : ICxmlPipelineStep
    {
        public int Order => CxmlPipelineOrder.Authenticate + 1;

        public Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
        {
            context.Items["tag"] = context.Partner?.Name;
            context.Notes = "tagged by " + context.Partner?.Name;
            return next();
        }
    }

    [Fact]
    public async Task Unexpected_handler_exception_becomes_a_logged_500()
    {
        await using var app = await TestApp.CreateAsync(configureServices: s => s.AddCxmlTransaction<ThrowingHandler>());

        var result = await app.PostAsync(Docs.Envelope(Docs.Request("<ExplodeRequest/>")));

        var status = TestApp.StatusOf(result);
        Assert.Equal(500, status.Code);
        Assert.Contains("kaboom", status.Detail);
        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Equal(500, log.CxmlStatusCode);
        Assert.Contains("InvalidOperationException", log.Notes);
        Assert.Equal("ExplodeRequest", log.DocumentType);
    }

    [Fact]
    public async Task Failed_handler_leaves_no_partial_changes_behind()
    {
        // A handler that saves, then throws: the pending state must not leak into the log save.
        await using var app = await TestApp.CreateAsync(configureServices: s => s.AddCxmlTransaction<ThrowingHandler>());

        await app.PostAsync(Docs.Envelope(Docs.Request("<ExplodeRequest/>")));
        await app.PostAsync(Docs.OrderRequest("PO-AFTER"));

        Assert.Single(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));
        Assert.Equal(2, await app.QueryAsync(db => db.MessageLogs.CountAsync()));
    }

    [Fact]
    public async Task New_transaction_registers_in_one_place_and_shows_up_in_the_profile()
    {
        await using var app = await TestApp.CreateAsync(configureServices: s => s
            .AddCxmlTransaction<PingPayload, PingCodec, PingHandler>()
            .AddDocumentRule<NoPingsRule>()
            .AddPipelineStep<TaggingStep>());

        var ping = await app.PostAsync(Docs.Envelope(Docs.Request("<PingRequest>hello</PingRequest>")), validateResponse: false);

        var status = TestApp.StatusOf(ping);
        Assert.Equal(200, status.Code);
        Assert.Equal("pong hello", status.Detail);
        var log = await app.GetLogAsync(ping.LogId!.Value);
        Assert.Contains(log.Issues, i => i.Code == "RULE-TEST-NO-PINGS");
        Assert.Equal("tagged by Demo Buyer", log.Notes);

        var profile = await app.PostAsync(Docs.Envelope(Docs.Request("<ProfileRequest/>")));
        var transactions = ((Cxml.Protocol.Documents.Profile.ProfileResponse)TestApp.Parse(profile.ResponseText).Response!.Payload!).Transactions;
        Assert.Contains(transactions, t => t.RequestName == "PingRequest");
    }

    [Fact]
    public void Built_in_rules_have_unique_codes_and_descriptions()
    {
        using var provider = new ServiceCollection().AddCxmlDevServerCore(_ => { }).BuildServiceProvider();

        var rules = provider.GetServices<IDocumentRule>().ToList();

        Assert.True(rules.Count >= 10);
        Assert.Equal(rules.Count, rules.Select(r => r.Code).Distinct().Count());
        Assert.All(rules, r =>
        {
            Assert.StartsWith("RULE-", r.Code);
            Assert.False(string.IsNullOrWhiteSpace(r.Description));
        });
    }

    [Fact]
    public async Task Pipeline_steps_run_in_the_documented_order()
    {
        await using var app = await TestApp.CreateAsync();

        var orders = await app.ScopeAsync(sp => Task.FromResult(sp.GetServices<ICxmlPipelineStep>().Select(s => s.Order).Order().ToList()));

        Assert.Equal(
            [CxmlPipelineOrder.Finalize, CxmlPipelineOrder.Capture, CxmlPipelineOrder.Parse, CxmlPipelineOrder.DtdValidate,
             CxmlPipelineOrder.Authenticate, CxmlPipelineOrder.Scenario, CxmlPipelineOrder.RuleValidate, CxmlPipelineOrder.Dispatch],
            orders);
    }
}
