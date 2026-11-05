using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Pipeline.Steps;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Tests;

public class InboundPipelineTests
{
    [Fact]
    public async Task Specification_example_order_is_accepted_and_persisted()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Samples", "0-OrderRequest.xml"));

        var result = await app.PostAsync(xml);

        Assert.Equal(200, result.HttpStatus);
        Assert.Equal(200, TestApp.StatusOf(result).Code);
        Assert.Equal("text/xml; charset=\"UTF-8\"", result.ContentType);

        var order = await app.QueryAsync(db => db.PurchaseOrders.AsNoTracking()
            .Include(o => o.Lines).Include(o => o.Partner).SingleAsync());
        Assert.Equal("DO1234", order.OrderId);
        Assert.Equal("Acme (spec example)", order.Partner!.Name);
        Assert.Equal(187.60m, order.TotalAmount);
        Assert.Equal("USD", order.Currency);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.Equal(3, order.Lines.Count);
        Assert.Equal([1, 4, 5], order.Lines.Select(l => l.LineNumber).Order());
        Assert.Equal(10 * 1.34m + 20 * 2.68m + 30 * 4.02m, order.Lines.Sum(l => l.LineTotal));
        Assert.Contains("DeliverTo", order.ShipToJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("32232995@ariba.acme.com", order.PayloadId);

        // The card number is never stored in clear text.
        Assert.DoesNotContain("1234567890123456", order.PaymentJson);
        Assert.Contains("************3456", order.PaymentJson);

        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Equal(MessageDirection.Inbound, log.Direction);
        Assert.Equal(MessageChannel.Http, log.Channel);
        Assert.Equal("OrderRequest", log.DocumentType);
        Assert.Equal("32232995@ariba.acme.com", log.PayloadId);
        Assert.Equal(200, log.CxmlStatusCode);
        Assert.Equal(order.PartnerId, log.PartnerId);
        Assert.Contains("<OrderRequest>", log.RawXml);
        Assert.Contains("<Status code=\"200\"", log.ResponseXml);
        Assert.Equal("order:" + order.PartnerId + ":DO1234", log.CorrelationId);
    }

    [Fact]
    public async Task Wrong_shared_secret_is_unauthorized()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(Docs.OrderRequest().Replace("devsecret", "wrong"));

        var status = TestApp.StatusOf(result);
        Assert.Equal(401, status.Code);
        Assert.Equal(200, result.HttpStatus);
        Assert.Empty(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));
        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Contains(log.Issues, i => i.Code == "AUTH-BAD-SECRET" && i.Source == ValidationSource.Auth);
        Assert.Equal(401, log.CxmlStatusCode);
    }

    [Fact]
    public async Task Missing_shared_secret_is_unauthorized_but_not_when_partner_does_not_require_one()
    {
        await using var app = await TestApp.CreateAsync();
        var noSecret = Docs.Envelope(Docs.Request(Docs.OrderRequestPayload()), secret: null);

        Assert.Equal(401, TestApp.StatusOf(await app.PostAsync(noSecret)).Code);

        await app.SetPartnerAsync("Demo Buyer", p => p.AuthMode = AuthMode.None);
        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(noSecret)).Code);
    }

    [Fact]
    public async Task Unknown_sender_is_unauthorized_unless_unknown_partners_are_allowed()
    {
        var xml = Docs.Envelope(
            Docs.Request(Docs.OrderRequestPayload("PO-X")),
            senderIdentity: "stranger", fromIdentity: "stranger");

        await using (var strict = await TestApp.CreateAsync())
        {
            var denied = await strict.PostAsync(xml);
            Assert.Equal(401, TestApp.StatusOf(denied).Code);
        }

        await using var open = await TestApp.CreateAsync(o => o.AllowUnknownPartners = true);
        var accepted = await open.PostAsync(xml);
        Assert.Equal(200, TestApp.StatusOf(accepted).Code);

        var log = await open.GetLogAsync(accepted.LogId!.Value);
        Assert.Null(log.PartnerId);
        Assert.Contains(log.Issues, i => i.Code == AuthenticateStep.UnknownPartnerCode && i.Severity == ValidationSeverity.Warning);
        Assert.Single(await open.QueryAsync(db => db.PurchaseOrders.Where(o => o.PartnerId == null).ToListAsync()));
    }

    [Fact]
    public async Task Partner_is_matched_by_from_credential_when_sender_is_unknown()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.Envelope(Docs.Request(Docs.OrderRequestPayload()), senderIdentity: "some-gateway");

        var result = await app.PostAsync(xml);

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Equal((await app.GetPartnerAsync("Demo Buyer")).Id, log.PartnerId);
        Assert.Contains(log.Issues, i => i.Code == AuthenticateStep.MatchedByFromCode);
    }

    [Fact]
    public async Task Credential_matching_ignores_case_and_surrounding_whitespace()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.Envelope(
            Docs.Request(Docs.OrderRequestPayload()),
            senderDomain: "networkid", senderIdentity: " an01000000001 ");

        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(xml)).Code);
    }

    [Fact]
    public async Task Inactive_partner_is_forbidden()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SetPartnerAsync("Demo Buyer", p => p.IsActive = false);

        var result = await app.PostAsync(Docs.OrderRequest());

        Assert.Equal(403, TestApp.StatusOf(result).Code);
    }

    [Fact]
    public async Task Wrong_to_identity_is_unauthorized_in_strict_and_a_warning_in_lenient()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.Envelope(Docs.Request(Docs.OrderRequestPayload()), toIdentity: "000000000");

        var lenient = await app.PostAsync(xml);
        Assert.Equal(200, TestApp.StatusOf(lenient).Code);
        Assert.Contains((await app.GetLogAsync(lenient.LogId!.Value)).Issues, i => i.Code == AuthenticateStep.ToMismatchCode);

        await app.SetPartnerAsync("Demo Buyer", p => p.ValidationMode = ValidationMode.Strict);
        Assert.Equal(401, TestApp.StatusOf(await app.PostAsync(xml)).Code);

        // The To identity may also be configured on the partner.
        await app.SetPartnerAsync("Demo Buyer", p => { p.ToDomain = "DUNS"; p.ToIdentity = "000000000"; });
        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(xml)).Code);
    }

    [Theory]
    [InlineData("not xml at all")]
    [InlineData("<cXML payloadID=\"x\"><Header>")]
    [InlineData("")]
    public async Task Malformed_xml_is_not_acceptable(string body)
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(body);

        var status = TestApp.StatusOf(result);
        Assert.Equal(406, status.Code);
        Assert.NotNull(status.Detail);
        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Contains(log.Issues, i => i.Code == ParseStep.ParseErrorCode && i.Severity == ValidationSeverity.Error);
        Assert.Equal(body, log.RawXml);
    }

    [Fact]
    public async Task Parse_errors_report_line_and_position()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync("<?xml version=\"1.0\"?>\n<cXML>\n  <Header>\n</cXML>");

        Assert.Contains("line 4", TestApp.StatusOf(result).Detail);
        var log = await app.GetLogAsync(result.LogId!.Value);
        Assert.Equal(4, log.Issues.Single().Line);
    }

    [Fact]
    public async Task Non_cxml_root_is_not_acceptable()
    {
        await using var app = await TestApp.CreateAsync();

        Assert.Equal(406, TestApp.StatusOf(await app.PostAsync("<html><body/></html>")).Code);
    }

    private static string OrderWithoutTotal() =>
        Docs.OrderRequest().Replace("<Total><Money currency=\"USD\">12.50</Money></Total>", string.Empty);

    [Fact]
    public async Task Dtd_errors_are_enforced_in_strict_mode_and_only_logged_in_lenient_mode()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = OrderWithoutTotal();
        Assert.False(TestApp.Validate(xml).IsValid, "test document must violate the DTD");

        var lenient = await app.PostAsync(xml);
        Assert.Equal(200, TestApp.StatusOf(lenient).Code);
        var lenientLog = await app.GetLogAsync(lenient.LogId!.Value);
        Assert.Contains(lenientLog.Issues, i => i.Source == ValidationSource.Dtd && i.Severity == ValidationSeverity.Error);

        await app.SetPartnerAsync("Demo Buyer", p => p.ValidationMode = ValidationMode.Strict);
        var strict = await app.PostAsync(xml.Replace("PO-1", "PO-2"));
        var status = TestApp.StatusOf(strict);
        Assert.Equal(406, status.Code);
        Assert.Contains("CXML-DTD", status.Detail);
        Assert.Single(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));
        var strictLog = await app.GetLogAsync(strict.LogId!.Value);
        Assert.NotEmpty(strictLog.Issues);
    }

    [Fact]
    public async Task Validation_mode_off_records_no_dtd_issues()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SetPartnerAsync("Demo Buyer", p => p.ValidationMode = ValidationMode.Off);

        var result = await app.PostAsync(OrderWithoutTotal());

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        Assert.Empty((await app.GetLogAsync(result.LogId!.Value)).Issues);
    }

    [Fact]
    public async Task Strict_mode_enforces_business_rules()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SetPartnerAsync("Demo Buyer", p => p.ValidationMode = ValidationMode.Strict);

        // A 'Z' suffix on the timestamp is not allowed by the specification.
        var xml = Docs.Envelope(Docs.Request(Docs.OrderRequestPayload()), timestamp: "2024-05-01T10:30:00Z");
        var result = await app.PostAsync(xml);

        var status = TestApp.StatusOf(result);
        Assert.Equal(406, status.Code);
        Assert.Contains("RULE-TIMESTAMP-FORMAT", status.Detail);
    }

    [Fact]
    public async Task Unsupported_request_is_not_implemented()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.Envelope(Docs.Request("<StatusUpdateRequest><Status code=\"200\" text=\"OK\"/></StatusUpdateRequest>"));

        var result = await app.PostAsync(xml);

        var status = TestApp.StatusOf(result);
        Assert.Equal(450, status.Code);
        Assert.Contains("StatusUpdateRequest", status.Detail);
        Assert.Contains("OrderRequest", status.Detail);
        Assert.Equal("StatusUpdateRequest", (await app.GetLogAsync(result.LogId!.Value)).DocumentType);
    }

    [Fact]
    public async Task Response_and_message_documents_are_rejected()
    {
        await using var app = await TestApp.CreateAsync();

        var response = "<?xml version=\"1.0\"?><cXML payloadID=\"1@h\" timestamp=\"2024-05-01T10:30:00+00:00\"><Response><Status code=\"200\" text=\"OK\"/></Response></cXML>";
        Assert.Equal(400, TestApp.StatusOf(await app.PostAsync(response)).Code);

        var message = Docs.Envelope("<Message><PunchOutOrderMessage><BuyerCookie>c</BuyerCookie><PunchOutOrderMessageHeader operationAllowed=\"create\"><Total><Money currency=\"USD\">0</Money></Total></PunchOutOrderMessageHeader></PunchOutOrderMessage></Message>");
        Assert.Equal(450, TestApp.StatusOf(await app.PostAsync(message)).Code);
    }

    [Fact]
    public async Task Form_encoded_documents_are_decoded()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.OrderRequest("PO-FORM");

        var fields = new List<KeyValuePair<string, string?>> { new("cXML-base64", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xml))) };
        var viaFields = await app.PostAsync("ignored", "application/x-www-form-urlencoded", formFields: fields);
        Assert.Equal(200, TestApp.StatusOf(viaFields).Code);

        var body = "cXML-urlencoded=" + Uri.EscapeDataString(Docs.OrderRequest("PO-FORM-2"));
        var viaBody = await app.PostAsync(body, "application/x-www-form-urlencoded");
        Assert.Equal(200, TestApp.StatusOf(viaBody).Code);

        var orders = await app.QueryAsync(db => db.PurchaseOrders.Select(o => o.OrderId).ToListAsync());
        Assert.Equal(["PO-FORM", "PO-FORM-2"], orders.Order());
        Assert.StartsWith("<?xml", (await app.GetLogAsync(viaFields.LogId!.Value)).RawXml);
    }

    [Fact]
    public async Task Invalid_base64_form_field_is_not_acceptable()
    {
        await using var app = await TestApp.CreateAsync();
        var fields = new List<KeyValuePair<string, string?>> { new("cXML-base64", "***not base64***") };

        var result = await app.PostAsync("x", "application/x-www-form-urlencoded", formFields: fields);

        Assert.Equal(406, TestApp.StatusOf(result).Code);
    }

    [Fact]
    public async Task Documents_in_other_encodings_are_decoded_using_the_xml_declaration()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.OrderRequest("PO-LATIN").Replace("encoding=\"UTF-8\"", "encoding=\"ISO-8859-1\"")
            .Replace("Buyer Inc", "Bürö Müller");
        var bytes = System.Text.Encoding.Latin1.GetBytes(xml);

        var result = await app.ScopeAsync(sp => sp.GetRequiredService<CxmlDevServer.Core.Pipeline.CxmlInboundProcessor>()
            .ProcessAsync(new CxmlDevServer.Core.Pipeline.CxmlInboundRequest { Body = bytes, BaseUrl = TestApp.BaseUrl }));

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var order = await app.QueryAsync(db => db.PurchaseOrders.SingleAsync());
        Assert.Contains("Bürö Müller", order.BillToJson);
    }

    [Fact]
    public async Task Every_response_has_a_fresh_payload_id_and_timestamp()
    {
        await using var app = await TestApp.CreateAsync();

        var first = TestApp.Parse((await app.PostAsync("garbage")).ResponseText);
        var second = TestApp.Parse((await app.PostAsync("garbage")).ResponseText);

        Assert.NotEqual(first.PayloadId, second.PayloadId);
        Assert.NotNull(first.TimestampText ?? first.Timestamp.ToString());
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$", first.Raw!.Attribute("timestamp")!.Value);
    }
}
