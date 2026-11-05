using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Templates;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Tests;

public class TemplateTests
{
    public static TheoryData<string> Keys => new()
    {
        TemplateKeys.PunchOutSetupCreate,
        TemplateKeys.PunchOutSetupEdit,
        TemplateKeys.PunchOutSetupInspect,
        TemplateKeys.OrderRequest,
        TemplateKeys.ProfileRequest,
    };

    private static Task<string> RenderAsync(TestApp app, string key, TradingPartner? partner) =>
        app.ScopeAsync(sp => sp.GetRequiredService<IMessageTemplateService>().RenderAsync(key, partner, TestApp.BaseUrl));

    [Fact]
    public async Task All_templates_are_listed()
    {
        await using var app = await TestApp.CreateAsync();
        var templates = await app.ScopeAsync(sp => Task.FromResult(sp.GetRequiredService<IMessageTemplateService>().Templates));

        Assert.Equal(
            [TemplateKeys.PunchOutSetupCreate, TemplateKeys.PunchOutSetupEdit, TemplateKeys.PunchOutSetupInspect, TemplateKeys.OrderRequest, TemplateKeys.ProfileRequest],
            templates.Select(t => t.Key));
        Assert.All(templates, t => Assert.False(string.IsNullOrWhiteSpace(t.Name)));
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public async Task Rendered_templates_are_dtd_valid_and_fully_filled(string key)
    {
        await using var app = await TestApp.CreateAsync();
        foreach (var partner in new[] { await app.GetPartnerAsync("Demo Buyer"), await app.GetPartnerAsync("Acme (spec example)"), null })
        {
            var xml = await RenderAsync(app, key, partner);

            var validation = TestApp.Validate(xml);
            Assert.True(validation.IsValid, string.Join('\n', validation.Issues) + xml);
            Assert.DoesNotContain("{{", xml);
            Assert.StartsWith("<?xml", xml);
            var envelope = TestApp.Parse(xml);
            Assert.NotEmpty(envelope.PayloadId);
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$", envelope.TimestampText!);
            Assert.Equal(partner?.FromIdentity ?? "AN01000000001", envelope.Header!.From.Primary!.Identity);
            Assert.Equal(partner?.SharedSecret ?? "devsecret", envelope.Header.Sender.Primary!.SharedSecret);
            Assert.Equal("123456789", envelope.Header.To.Primary!.Identity);
        }
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public async Task Rendered_templates_are_accepted_by_the_pipeline_even_in_strict_mode(string key)
    {
        await using var app = await TestApp.CreateAsync();
        await app.SetPartnerAsync("Demo Buyer", p => p.ValidationMode = ValidationMode.Strict);
        await app.SetPartnerAsync("Acme (spec example)", p => p.ValidationMode = ValidationMode.Strict);

        foreach (var name in new[] { "Demo Buyer", "Acme (spec example)" })
        {
            var partner = await app.GetPartnerAsync(name);
            var result = await app.PostAsync(await RenderAsync(app, key, partner));

            var status = TestApp.StatusOf(result);
            Assert.True(status.Code == 200, $"{key} for {name}: {status.Code} {status.Detail}");
            var log = await app.GetLogAsync(result.LogId!.Value);
            Assert.Equal(partner.Id, log.PartnerId);
            Assert.DoesNotContain(log.Issues, i => i.Severity == ValidationSeverity.Error);
        }
    }

    [Fact]
    public async Task Order_template_round_trips_into_a_purchase_order()
    {
        await using var app = await TestApp.CreateAsync();
        var partner = await app.GetPartnerAsync("Demo Buyer");

        var first = await RenderAsync(app, TemplateKeys.OrderRequest, partner);
        var second = await RenderAsync(app, TemplateKeys.OrderRequest, partner);
        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(first)).Code);
        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(second)).Code);

        var orders = await app.QueryAsync(db => db.PurchaseOrders.Include(o => o.Lines).ToListAsync());
        Assert.Equal(2, orders.Count); // every rendering gets its own orderID
        Assert.All(orders, o =>
        {
            Assert.Equal(partner.Id, o.PartnerId);
            Assert.Equal(187.60m, o.TotalAmount);
            Assert.Equal(3, o.Lines.Count);
        });
    }

    [Fact]
    public async Task Punchout_templates_point_back_at_the_test_buyer_receiver_and_use_catalog_items()
    {
        await using var app = await TestApp.CreateAsync();
        var partner = await app.GetPartnerAsync("Demo Buyer");

        var create = TestApp.Parse(await RenderAsync(app, TemplateKeys.PunchOutSetupCreate, partner));
        var request = (PunchOutSetupRequest)create.Request!.Payload!;
        Assert.Equal(PunchOutOperation.Create, request.Operation);
        Assert.Equal(TestApp.BaseUrl + "/test-buyer/punchout-return", request.BrowserFormPostUrl);
        Assert.StartsWith("bc-", request.BuyerCookie);

        var edit = (PunchOutSetupRequest)TestApp.Parse(await RenderAsync(app, TemplateKeys.PunchOutSetupEdit, partner)).Request!.Payload!;
        Assert.Equal(PunchOutOperation.Edit, edit.Operation);
        Assert.Equal(2, edit.Items.Count);
        Assert.Equal("LAP-1001", edit.Items[0].ItemId.SupplierPartId);
        Assert.Equal("cxmldev:1", edit.Items[0].ItemId.SupplierPartAuxiliaryId);

        var inspect = (PunchOutSetupRequest)TestApp.Parse(await RenderAsync(app, TemplateKeys.PunchOutSetupInspect, partner)).Request!.Payload!;
        Assert.Equal(PunchOutOperation.Inspect, inspect.Operation);
        Assert.Null(inspect.BrowserFormPostUrl);

        Assert.NotEqual(create.PayloadId, TestApp.Parse(await RenderAsync(app, TemplateKeys.PunchOutSetupCreate, partner)).PayloadId);
    }

    [Fact]
    public async Task Edit_template_builds_a_cart_of_catalog_items()
    {
        await using var app = await TestApp.CreateAsync();
        var result = await app.PostAsync(await RenderAsync(app, TemplateKeys.PunchOutSetupEdit, await app.GetPartnerAsync("Demo Buyer")));

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var lines = await app.QueryAsync(db => db.PunchOutCartLines.AsNoTracking().OrderBy(l => l.LineNumber).ToListAsync());
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.False(l.IsAdHoc));
        Assert.Equal([2m, 1m], lines.Select(l => l.Quantity));
    }

    [Fact]
    public async Task Values_are_xml_escaped()
    {
        await using var app = await TestApp.CreateAsync();
        var partner = await app.GetPartnerAsync("Demo Buyer");
        partner.SharedSecret = "a&b<c>\"d'";
        await app.SetPartnerAsync("Demo Buyer", p => p.SharedSecret = partner.SharedSecret);

        var xml = await RenderAsync(app, TemplateKeys.ProfileRequest, partner);

        Assert.Equal(partner.SharedSecret, TestApp.Parse(xml).Header!.Sender.Primary!.SharedSecret);
        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(xml)).Code);
    }

    [Fact]
    public async Task Partner_to_override_is_used_and_unknown_keys_are_refused()
    {
        await using var app = await TestApp.CreateAsync();
        var partner = await app.GetPartnerAsync("Demo Buyer");
        partner.ToDomain = "DUNS";
        partner.ToIdentity = "942888711";

        var xml = await RenderAsync(app, TemplateKeys.ProfileRequest, partner);

        Assert.Equal("942888711", TestApp.Parse(xml).Header!.To.Primary!.Identity);
        await Assert.ThrowsAsync<ArgumentException>(() => RenderAsync(app, "nope", partner));
    }
}
