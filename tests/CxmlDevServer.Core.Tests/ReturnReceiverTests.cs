using System.Net;
using System.Text;
using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Messaging;
using CxmlDevServer.Core.PunchOut;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Tests;

public class ReturnReceiverTests
{
    private static Task<PunchOutReturnResult> ReceiveAsync(TestApp app, params (string Name, string? Value)[] fields) =>
        app.ScopeAsync(sp => sp.GetRequiredService<PunchOutReturnReceiver>().ReceiveAsync(
            fields.Select(f => new KeyValuePair<string, string?>(f.Name, f.Value)).ToList(), "10.0.0.5:5555"));

    private static async Task<(string Token, PunchOutReturn Back, CatalogItem Item)> CheckedOutAsync(TestApp app, string partnerName)
    {
        var partner = await app.GetPartnerAsync(partnerName);
        var setup = Docs.Envelope(
            Docs.Request("""
                <PunchOutSetupRequest operation="create">
                  <BuyerCookie>cookie-rcv</BuyerCookie>
                  <BrowserFormPost><URL>http://localhost:5050/test-buyer/punchout-return</URL></BrowserFormPost>
                </PunchOutSetupRequest>
                """),
            senderDomain: partner.SenderDomain, senderIdentity: partner.SenderIdentity, secret: partner.SharedSecret,
            fromDomain: partner.FromDomain, fromIdentity: partner.FromIdentity, payloadId: "setup-rcv@buyer");
        var response = await app.PostAsync(setup);
        var token = ((PunchOutSetupResponse)TestApp.Parse(response.ResponseText).Response!.Payload!).StartPageUrl.Split('/').Last();
        var item = await app.QueryAsync(db => db.CatalogItems.AsNoTracking().OrderBy(i => i.Id).FirstAsync());
        var back = await app.ScopeAsync(async sp =>
        {
            var service = sp.GetRequiredService<PunchOutService>();
            await service.AddToCartAsync(token, item.Id, 3);
            return await service.CheckoutAsync(token);
        });
        return (token, back, item);
    }

    [Theory]
    [InlineData("Demo Buyer", "cXML-base64")]
    [InlineData("Acme (spec example)", "cXML-urlencoded")]
    public async Task Order_message_posted_back_is_decoded_validated_and_logged(string partnerName, string expectedField)
    {
        await using var app = await TestApp.CreateAsync();
        var (token, back, item) = await CheckedOutAsync(app, partnerName);
        Assert.Equal(expectedField, back.FieldName);

        // What the browser posts: base64 as is, or the HTML attribute value decoded back to text.
        var posted = back.FieldName == "cXML-urlencoded" ? WebUtility.HtmlDecode(back.FieldValue) : back.FieldValue;
        var result = await ReceiveAsync(app, (back.FieldName, posted));

        Assert.True(result.Decoded);
        Assert.True(result.IsValid, string.Join('\n', result.Issues));
        Assert.Equal(back.Xml, result.Xml);
        Assert.Equal(back.FieldName == "cXML-urlencoded" ? PunchOutFormEncoding.UrlEncoded : PunchOutFormEncoding.Base64, result.Encoding);
        Assert.NotNull(result.Message);
        var summary = result.Summary!;
        Assert.Equal("cookie-rcv", summary.BuyerCookie);
        Assert.Equal("setup-rcv@buyer", summary.InReplyTo);
        Assert.Equal(1, summary.ItemCount);
        Assert.Equal(3 * item.UnitPrice, summary.TotalAmount);
        Assert.Equal(summary.TotalAmount, summary.CalculatedTotal);
        Assert.Equal("USD", summary.Currency);

        Assert.Equal(token, result.MatchedSession!.Token);

        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Equal(MessageDirection.Inbound, log.Direction);
        Assert.Equal(MessageChannel.BrowserPost, log.Channel);
        Assert.Equal("PunchOutOrderMessage", log.DocumentType);
        Assert.Equal(result.MatchedSession.CorrelationId, log.CorrelationId);
        Assert.Equal(result.MatchedSession.PartnerId, log.PartnerId);
        Assert.Equal(back.Xml, log.RawXml);
        Assert.Equal("setup-rcv@buyer", log.InReplyTo);
        Assert.Equal("10.0.0.5:5555", log.RemoteEndpoint);

        // The session's messages are linked: setup request, outbound order message, inbound receipt.
        var correlated = await app.QueryAsync(db => db.MessageLogs.CountAsync(l => l.CorrelationId == log.CorrelationId));
        Assert.Equal(3, correlated);
    }

    [Fact]
    public async Task Invalid_documents_are_reported_with_issues()
    {
        await using var app = await TestApp.CreateAsync();
        var (_, back, _) = await CheckedOutAsync(app, "Demo Buyer");
        var broken = back.Xml.Replace("<UnitOfMeasure>EA</UnitOfMeasure>", string.Empty);

        var result = await ReceiveAsync(app, ("cXML-base64", Convert.ToBase64String(Encoding.UTF8.GetBytes(broken))));

        Assert.True(result.Decoded);
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Source == ValidationSource.Dtd && i.Severity == ValidationSeverity.Error);
        Assert.NotEmpty((await app.GetLogAsync(result.LogId!.Value)).Issues);
    }

    [Theory]
    [InlineData("nothing here", null)]
    [InlineData("cXML-base64", "***")]
    [InlineData("cXML-base64", "aGVsbG8=")] // "hello": decodes, but is not XML
    public async Task Unreadable_posts_are_reported_and_logged(string field, string? value)
    {
        await using var app = await TestApp.CreateAsync();

        var result = await ReceiveAsync(app, (field, value));

        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.NotNull(result.LogId);
        var log = await app.GetLogAsync(result.LogId.Value);
        Assert.NotEmpty(log.Issues);
        Assert.Equal(result.Error, log.Notes);
    }

    [Fact]
    public async Task Other_documents_are_flagged_as_not_an_order_message()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await ReceiveAsync(app, ("cXML-base64", Convert.ToBase64String(Encoding.UTF8.GetBytes(Docs.OrderRequest()))));

        Assert.True(result.Decoded);
        Assert.Null(result.Message);
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "PUNCHOUT-RETURN-NOT-ORDERMESSAGE");
    }

    [Fact]
    public async Task Unmatched_messages_have_no_session()
    {
        await using var app = await TestApp.CreateAsync();
        var (_, back, _) = await CheckedOutAsync(app, "Demo Buyer");
        var other = back.Xml.Replace("setup-rcv@buyer", "somebody-else").Replace("cookie-rcv", "other-cookie");

        var result = await ReceiveAsync(app, ("cXML-base64", Convert.ToBase64String(Encoding.UTF8.GetBytes(other))));

        Assert.True(result.IsValid);
        Assert.Null(result.MatchedSession);
    }
}
