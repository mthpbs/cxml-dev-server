using System.Net;
using System.Text;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Messaging;
using CxmlDevServer.Core.Pipeline;
using CxmlDevServer.Core.Templates;
using CxmlDevServer.Core.Tests.Support;
using CxmlDevServer.Infrastructure.Outbound;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Tests;

public class OutboundClientTests
{
    /// <summary>Delivers outbound posts to the in-process pipeline instead of the network.</summary>
    private sealed class LoopbackHandler : HttpMessageHandler
    {
        public Func<TestApp>? App { get; set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var body = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            LastBody = Encoding.UTF8.GetString(body);
            if (request.RequestUri!.Host == "unreachable.test")
            {
                throw new HttpRequestException("No such host is known.");
            }

            if (request.RequestUri.Host == "html.test")
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>Bad Gateway</html>", Encoding.UTF8, "text/html") };
            }

            var result = await App!().ScopeAsync(sp => sp.GetRequiredService<CxmlInboundProcessor>().ProcessAsync(new CxmlInboundRequest
            {
                Body = body,
                ContentType = request.Content.Headers.ContentType?.ToString(),
                BaseUrl = TestApp.BaseUrl,
            }, cancellationToken));
            return new HttpResponseMessage((HttpStatusCode)result.HttpStatus)
            {
                Content = new StringContent(result.ResponseText, new UTF8Encoding(false), "text/xml"),
            };
        }
    }

    private static async Task<(TestApp App, LoopbackHandler Handler)> CreateAsync()
    {
        var handler = new LoopbackHandler();
        TestApp? app = null;
        handler.App = () => app!;
        app = await TestApp.CreateAsync(configureServices: s =>
            s.AddHttpClient(CxmlOutboundClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler));
        return (app, handler);
    }

    private static Task<CxmlOutboundResult> SendAsync(TestApp app, string url, string xml) =>
        app.ScopeAsync(sp => sp.GetRequiredService<ICxmlOutboundClient>().SendAsync(url, xml));

    [Fact]
    public async Task Order_is_sent_logged_and_correlated_with_the_inbound_processing()
    {
        var (app, handler) = await CreateAsync();
        await using var _ = app;
        var partner = await app.GetPartnerAsync("Demo Buyer");
        var xml = await app.ScopeAsync(sp => sp.GetRequiredService<IMessageTemplateService>()
            .RenderAsync(TemplateKeys.OrderRequest, partner, TestApp.BaseUrl));

        var result = await SendAsync(app, TestApp.BaseUrl + "/cxml", xml);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(200, result.HttpStatus);
        Assert.Equal(200, result.CxmlStatusCode);
        Assert.Equal("OK", result.CxmlStatusText);
        Assert.NotNull(result.ResponseEnvelope?.Response);
        Assert.True(result.Duration >= TimeSpan.Zero);
        Assert.Equal("text/xml; charset=UTF-8", handler.LastRequest!.Content!.Headers.ContentType!.ToString());
        Assert.Equal(xml, handler.LastBody);
        Assert.Single(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));

        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Equal(MessageDirection.Outbound, log.Direction);
        Assert.Equal(MessageChannel.ManualSend, log.Channel);
        Assert.Equal("OrderRequest", log.DocumentType);
        Assert.Equal(TestApp.BaseUrl + "/cxml", log.RemoteEndpoint);
        Assert.Equal(xml, log.RawXml);
        Assert.Equal(result.ResponseText, log.ResponseXml);
        Assert.Equal(200, log.HttpStatus);
        Assert.Equal(200, log.CxmlStatusCode);
        Assert.Equal(partner.Id, log.PartnerId);

        // The outbound entry and the server's own inbound entry for the same order share a correlation id.
        var inbound = await app.QueryAsync(db => db.MessageLogs.AsNoTracking().SingleAsync(l => l.Direction == MessageDirection.Inbound));
        Assert.Equal(inbound.CorrelationId, log.CorrelationId);
        Assert.Equal(inbound.PayloadId, log.PayloadId);
    }

    [Fact]
    public async Task Punchout_setup_sent_manually_is_correlated_with_its_session()
    {
        var (app, _) = await CreateAsync();
        await using var _ = app;
        var xml = await app.ScopeAsync(sp => sp.GetRequiredService<IMessageTemplateService>()
            .RenderAsync(TemplateKeys.PunchOutSetupCreate, null, TestApp.BaseUrl));

        var result = await SendAsync(app, TestApp.BaseUrl + "/cxml", xml);

        Assert.Equal(200, result.CxmlStatusCode);
        var session = await app.QueryAsync(db => db.PunchOutSessions.AsNoTracking().SingleAsync());
        Assert.Equal(session.CorrelationId, (await app.GetLogAsync(result.LogId!.Value)).CorrelationId);
    }

    [Fact]
    public async Task Cxml_error_responses_are_reported_with_their_status()
    {
        var (app, _) = await CreateAsync();
        await using var _ = app;

        var result = await SendAsync(app, TestApp.BaseUrl + "/cxml", Docs.OrderRequest().Replace("devsecret", "bad"));

        Assert.True(result.Succeeded);
        Assert.Equal(401, result.CxmlStatusCode);
        Assert.Equal(401, (await app.GetLogAsync(result.LogId!.Value)).CxmlStatusCode);
    }

    [Fact]
    public async Task Non_cxml_responses_are_kept_as_text()
    {
        var (app, _) = await CreateAsync();
        await using var _ = app;

        var result = await SendAsync(app, "http://html.test/cxml", Docs.OrderRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(502, result.HttpStatus);
        Assert.Null(result.ResponseEnvelope);
        Assert.Null(result.CxmlStatusCode);
        Assert.Equal("<html>Bad Gateway</html>", result.ResponseText);
        Assert.Equal("<html>Bad Gateway</html>", (await app.GetLogAsync(result.LogId!.Value)).ResponseXml);
    }

    [Theory]
    [InlineData("http://unreachable.test/cxml")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/cxml")]
    public async Task Failures_are_returned_and_logged_not_thrown(string url)
    {
        var (app, _) = await CreateAsync();
        await using var _ = app;

        var result = await SendAsync(app, url, Docs.OrderRequest());

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Null(result.HttpStatus);
        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Equal(result.Error, log.Notes);
        Assert.Null(log.HttpStatus);
        Assert.Equal(MessageChannel.ManualSend, log.Channel);
    }

    [Fact]
    public async Task Unparseable_documents_can_still_be_sent()
    {
        var (app, handler) = await CreateAsync();
        await using var _ = app;

        var result = await SendAsync(app, TestApp.BaseUrl + "/cxml", "<broken");

        Assert.Equal(406, result.CxmlStatusCode);
        Assert.Equal("<broken", handler.LastBody);
        Assert.Equal("Unknown", (await app.GetLogAsync(result.LogId!.Value)).DocumentType);
    }
}
