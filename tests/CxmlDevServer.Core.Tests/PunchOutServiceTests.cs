using System.Net;
using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.PunchOut;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Tests;

public class PunchOutServiceTests
{
    private static async Task<string> SetupAsync(
        TestApp app, string operation = "create", string cookie = "cookie-1", string? items = null, string payloadId = "setup@buyer",
        string? browserFormPost = "https://buyer.example/return", string deploymentMode = "test")
    {
        var result = await app.PostAsync(Docs.PunchOutSetup(
            operation, cookie, operation == "inspect" ? null : browserFormPost, items ?? string.Empty, deploymentMode, payloadId));
        Assert.Equal(200, TestApp.StatusOf(result).Code);
        return ((PunchOutSetupResponse)TestApp.Parse(result.ResponseText).Response!.Payload!).StartPageUrl.Split('/').Last();
    }

    private static Task<T> WithService<T>(TestApp app, Func<PunchOutService, Task<T>> action) =>
        app.ScopeAsync(sp => action(sp.GetRequiredService<PunchOutService>()));

    private static Task Run(TestApp app, Func<PunchOutService, Task> action) =>
        app.ScopeAsync(async sp =>
        {
            await action(sp.GetRequiredService<PunchOutService>());
            return 0;
        });

    private static Task<List<CatalogItem>> CatalogAsync(TestApp app, int count) =>
        app.QueryAsync(db => db.CatalogItems.AsNoTracking().OrderBy(i => i.Id).Take(count).ToListAsync());

    [Fact]
    public async Task Session_is_opened_once_and_can_be_reloaded()
    {
        await using var app = await TestApp.CreateAsync();
        var token = await SetupAsync(app);

        var first = await WithService(app, s => s.OpenAsync(token));
        Assert.Equal(PunchOutSessionStatus.Opened, first.Status);
        var openedAt = first.OpenedAt;
        Assert.NotNull(openedAt);

        var second = await WithService(app, s => s.OpenAsync(token));
        Assert.Equal(PunchOutSessionStatus.Opened, second.Status);
        Assert.Equal(openedAt, second.OpenedAt);
        Assert.NotNull(await WithService(app, s => s.GetActiveSessionAsync(token)));
    }

    [Fact]
    public async Task Unknown_token_has_no_session()
    {
        await using var app = await TestApp.CreateAsync();

        Assert.Null(await WithService(app, s => s.GetSessionAsync("nope")));
        Assert.Null(await WithService(app, s => s.GetActiveSessionAsync("nope")));
        var ex = await Assert.ThrowsAsync<PunchOutSessionException>(() => WithService(app, s => s.OpenAsync("nope")));
        Assert.Equal(PunchOutError.NotFound, ex.Error);
    }

    [Fact]
    public async Task Expired_sessions_are_not_returned_as_active()
    {
        await using var app = await TestApp.CreateAsync();
        var token = await SetupAsync(app);
        await app.ModifyAsync(async db => (await db.PunchOutSessions.SingleAsync()).ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.Null(await WithService(app, s => s.GetActiveSessionAsync(token)));
        var session = await WithService(app, s => s.GetSessionAsync(token));
        Assert.Equal(PunchOutSessionStatus.Expired, session!.Status);
        Assert.Equal(PunchOutSessionStatus.Expired, (await app.QueryAsync(db => db.PunchOutSessions.SingleAsync())).Status);

        var ex = await Assert.ThrowsAsync<PunchOutSessionException>(() => WithService(app, s => s.OpenAsync(token)));
        Assert.Equal(PunchOutError.Expired, ex.Error);
        ex = await Assert.ThrowsAsync<PunchOutSessionException>(() => WithService(app, s => s.AddToCartAsync(token, 1)));
        Assert.Equal(PunchOutError.Expired, ex.Error);
    }

    [Fact]
    public async Task Cart_lines_can_be_added_changed_and_removed()
    {
        await using var app = await TestApp.CreateAsync();
        var token = await SetupAsync(app);
        var items = await CatalogAsync(app, 2);

        var line1 = await WithService(app, s => s.AddToCartAsync(token, items[0].Id, 2));
        await WithService(app, s => s.AddToCartAsync(token, items[1].Id));
        await WithService(app, s => s.AddToCartAsync(token, items[0].Id, 3)); // merges into the existing line

        var session = (await WithService(app, s => s.GetSessionAsync(token)))!;
        Assert.Equal(2, session.Lines.Count);
        Assert.Equal(5, session.Lines.Single(l => l.CatalogItemId == items[0].Id).Quantity);
        Assert.Equal(items[0].UnitPrice, session.Lines.First().UnitPrice);
        Assert.Equal(PunchOutCookies.ForCatalogItem(items[0].Id), session.Lines.First().SupplierPartAuxiliaryId);

        await Run(app, s => s.SetQuantityAsync(token, line1.Id, 7));
        session = (await WithService(app, s => s.GetSessionAsync(token)))!;
        Assert.Equal(7, session.Lines.Single(l => l.Id == line1.Id).Quantity);

        await Run(app, s => s.SetQuantityAsync(token, line1.Id, 0)); // zero removes
        session = (await WithService(app, s => s.GetSessionAsync(token)))!;
        Assert.Equal(items[1].Id, Assert.Single(session.Lines).CatalogItemId);

        await Run(app, s => s.ClearCartAsync(token));
        Assert.Empty((await WithService(app, s => s.GetSessionAsync(token)))!.Lines);
        Assert.Equal(0, await app.QueryAsync(db => db.PunchOutCartLines.CountAsync()));
    }

    [Fact]
    public async Task Invalid_cart_operations_are_refused()
    {
        await using var app = await TestApp.CreateAsync();
        var token = await SetupAsync(app);

        var ex = await Assert.ThrowsAsync<PunchOutSessionException>(() => WithService(app, s => s.AddToCartAsync(token, 9999)));
        Assert.Equal(PunchOutError.ItemNotFound, ex.Error);
        ex = await Assert.ThrowsAsync<PunchOutSessionException>(() => WithService(app, s => s.AddToCartAsync(token, 1, 0)));
        Assert.Equal(PunchOutError.Invalid, ex.Error);
        ex = await Assert.ThrowsAsync<PunchOutSessionException>(() => Run(app, s => s.RemoveLineAsync(token, 12345)));
        Assert.Equal(PunchOutError.ItemNotFound, ex.Error);
    }

    [Fact]
    public async Task Inspect_sessions_are_read_only()
    {
        await using var app = await TestApp.CreateAsync();
        var items = await CatalogAsync(app, 1);
        var token = await SetupAsync(app, "inspect", items: Docs.ItemOut(items[0].SupplierPartId, 1));

        foreach (var action in new Func<PunchOutService, Task>[]
                 {
                     s => s.AddToCartAsync(token, items[0].Id),
                     s => s.ClearCartAsync(token),
                     s => s.CheckoutAsync(token),
                 })
        {
            var ex = await Assert.ThrowsAsync<PunchOutSessionException>(() => Run(app, action));
            Assert.Equal(PunchOutError.ReadOnly, ex.Error);
        }

        // Leaving an inspect session just closes it; there is nowhere to post to.
        Assert.Null(await WithService(app, s => s.CancelAsync(token)));
        Assert.Equal(PunchOutSessionStatus.Cancelled, (await app.QueryAsync(db => db.PunchOutSessions.SingleAsync())).Status);
    }

    [Fact]
    public async Task Checkout_returns_a_valid_order_message_and_completes_the_session()
    {
        await using var app = await TestApp.CreateAsync();
        const string cookie = "<Extrinsic name=\"session\">7</Extrinsic>";
        var token = await SetupAsync(app, cookie: cookie, payloadId: "setup-77@buyer", deploymentMode: "test");
        var items = await CatalogAsync(app, 2);
        await WithService(app, s => s.AddToCartAsync(token, items[0].Id, 2));
        await WithService(app, s => s.AddToCartAsync(token, items[1].Id, 1));

        var back = await WithService(app, s => s.CheckoutAsync(token));

        Assert.Equal("https://buyer.example/return", back.FormActionUrl);
        Assert.False(back.Cancelled);
        Assert.Equal("cXML-base64", back.FieldName); // Demo Buyer prefers base64

        // The form value decodes back to exactly the logged document.
        Assert.Equal(back.Xml, PunchOutFormEncoder.Decode([new KeyValuePair<string, string?>(back.FieldName, back.FieldValue)]));
        var validation = TestApp.Validate(back.Xml);
        Assert.True(validation.IsValid, string.Join('\n', validation.Issues) + back.Xml);
        Assert.StartsWith("<?xml", back.Xml);

        var envelope = TestApp.Parse(back.Xml);
        Assert.Equal("setup-77@buyer", envelope.Message!.InReplyTo);
        Assert.Equal("test", envelope.Message.DeploymentMode);
        Assert.Null(envelope.Message.Status);
        var message = (PunchOutOrderMessage)envelope.Message.Payload!;
        Assert.Equal(cookie, message.BuyerCookie);
        Assert.Equal(PunchOutOperation.Edit, message.Header.OperationAllowed); // from the Demo Buyer partner
        var expectedTotal = 2 * items[0].UnitPrice + items[1].UnitPrice;
        Assert.Equal(expectedTotal, message.Header.Total.Amount);
        Assert.Equal("USD", message.Header.Total.Currency);
        Assert.Equal(2, message.Items.Count);
        Assert.Equal([1, 2], message.Items.Select(i => i.LineNumber!.Value));
        Assert.Equal(items[0].SupplierPartId, message.Items[0].ItemId.SupplierPartId);
        Assert.Equal(PunchOutCookies.ForCatalogItem(items[0].Id), message.Items[0].ItemId.SupplierPartAuxiliaryId);
        Assert.Equal(2, message.Items[0].Quantity);
        Assert.Equal(items[0].UnitPrice, message.Items[0].ItemDetail.UnitPrice.Amount);
        Assert.Equal(items[0].ShortName, message.Items[0].ItemDetail.Descriptions[0].ShortName);
        Assert.Equal("UNSPSC", message.Items[0].ItemDetail.Classifications[0].Domain);
        Assert.Equal(items[0].Unspsc, message.Items[0].ItemDetail.Classifications[0].Value);
        Assert.Equal(items[0].ManufacturerName, message.Items[0].ItemDetail.ManufacturerName);
        Assert.Equal(items[0].LeadTimeDays, message.Items[0].ItemDetail.LeadTime);

        // Header: from the supplier to the buyer, the supplier never sends a secret.
        Assert.Equal("DUNS", envelope.Header!.From.Primary!.Domain);
        Assert.Equal("123456789", envelope.Header.From.Primary.Identity);
        Assert.Equal("NetworkId", envelope.Header.To.Primary!.Domain);
        Assert.Equal("AN01000000001", envelope.Header.To.Primary.Identity);
        Assert.Equal("cXML Dev Server 1.0", envelope.Header.Sender.UserAgent);
        Assert.All(envelope.Header.Sender.Credentials, c => Assert.Null(c.SharedSecret));
        Assert.DoesNotContain("SharedSecret", back.Xml);

        var session = await app.QueryAsync(db => db.PunchOutSessions.AsNoTracking().SingleAsync());
        Assert.Equal(PunchOutSessionStatus.Submitted, session.Status);
        Assert.NotNull(session.CompletedAt);
        Assert.Null(await WithService(app, s => s.GetActiveSessionAsync(token)));

        var log = await app.GetLogAsync(back.LogId);
        Assert.Equal(MessageDirection.Outbound, log.Direction);
        Assert.Equal(MessageChannel.BrowserPost, log.Channel);
        Assert.Equal("PunchOutOrderMessage", log.DocumentType);
        Assert.Equal(session.CorrelationId, log.CorrelationId);
        Assert.Equal("setup-77@buyer", log.InReplyTo);
        Assert.Equal(back.Xml, log.RawXml);

        // A second checkout is refused.
        var ex = await Assert.ThrowsAsync<PunchOutSessionException>(() => WithService(app, s => s.CheckoutAsync(token)));
        Assert.Equal(PunchOutError.Completed, ex.Error);
    }

    [Fact]
    public async Task Checkout_encodes_according_to_the_partner_setting()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SetPartnerAsync("Demo Buyer", p => p.PunchOutPostEncoding = PunchOutPostEncoding.UrlEncoded);
        // A cookie with characters that need HTML entities, and a non-ASCII item description.
        var token = await SetupAsync(app, cookie: "a&amp;b &lt;tag&gt; \"q\" ü");
        await app.ModifyAsync(async db => (await db.CatalogItems.OrderBy(i => i.Id).FirstAsync()).Description = "Größe & \"Qualität\" <b>");
        var item = (await CatalogAsync(app, 1))[0];
        await WithService(app, s => s.AddToCartAsync(token, item.Id));

        var back = await WithService(app, s => s.CheckoutAsync(token));

        Assert.Equal("cXML-urlencoded", back.FieldName);
        Assert.All(back.FieldValue, c => Assert.True(c < 0x80, "urlencoded value must be us-ascii"));
        Assert.DoesNotContain('<', back.FieldValue);
        Assert.DoesNotContain('"', back.FieldValue);
        // Writing the value into an HTML attribute and decoding it (what the browser does) restores the document.
        Assert.Equal(back.Xml, WebUtility.HtmlDecode(back.FieldValue));
        Assert.Equal(back.Xml, PunchOutFormEncoder.DecodeUrlEncodedFieldValue(back.FieldValue));
        Assert.Contains("Größe", back.Xml);
        Assert.True(TestApp.Validate(back.Xml).IsValid);
        Assert.Equal("a&amp;b &lt;tag&gt; \"q\" ü", ((PunchOutOrderMessage)TestApp.Parse(back.Xml).Message!.Payload!).BuyerCookie);
    }

    [Fact]
    public async Task Cancelling_a_create_session_returns_an_empty_cart()
    {
        await using var app = await TestApp.CreateAsync();
        var token = await SetupAsync(app);
        var item = (await CatalogAsync(app, 1))[0];
        await WithService(app, s => s.AddToCartAsync(token, item.Id, 4));

        var back = await WithService(app, s => s.CancelAsync(token));

        Assert.NotNull(back);
        Assert.True(back.Cancelled);
        var envelope = TestApp.Parse(back.Xml);
        var message = (PunchOutOrderMessage)envelope.Message!.Payload!;
        Assert.Empty(message.Items);
        Assert.Equal(0m, message.Header.Total.Amount);
        Assert.Null(envelope.Message.Status);
        Assert.True(TestApp.Validate(back.Xml).IsValid);
        Assert.Equal(PunchOutSessionStatus.Cancelled, (await app.QueryAsync(db => db.PunchOutSessions.SingleAsync())).Status);
        Assert.Null(await WithService(app, s => s.GetActiveSessionAsync(token)));
    }

    [Fact]
    public async Task Cancelling_an_edit_session_returns_the_original_items_with_status_204()
    {
        await using var app = await TestApp.CreateAsync();
        var items = await CatalogAsync(app, 2);
        var token = await SetupAsync(app, "edit", items: Docs.ItemOut(items[0].SupplierPartId, 2)
                                                         + Docs.ItemOut("ADHOC-1", 1, aux: "<Extrinsic name=\"ref\">7</Extrinsic>", price: 9.99m, name: "Custom"));
        // The user changes the cart, then changes their mind.
        await WithService(app, s => s.AddToCartAsync(token, items[1].Id, 5));
        await Run(app, s => s.ClearCartAsync(token));

        var back = await WithService(app, s => s.CancelAsync(token));

        Assert.NotNull(back);
        var envelope = TestApp.Parse(back.Xml);
        Assert.Equal(204, envelope.Message!.Status!.Code);
        var message = (PunchOutOrderMessage)envelope.Message.Payload!;
        Assert.Equal(2, message.Items.Count);
        Assert.Equal(items[0].SupplierPartId, message.Items[0].ItemId.SupplierPartId);
        Assert.Equal(2, message.Items[0].Quantity);
        Assert.Equal("ADHOC-1", message.Items[1].ItemId.SupplierPartId);
        Assert.Equal("<Extrinsic name=\"ref\">7</Extrinsic>", message.Items[1].ItemId.SupplierPartAuxiliaryId);
        Assert.Equal(2 * items[0].UnitPrice + 9.99m, message.Header.Total.Amount);
        var check = TestApp.Validate(back.Xml);
        Assert.True(check.IsValid, string.Join('\n', check.Issues) + back.Xml);
    }

    [Fact]
    public async Task Edit_session_checkout_returns_the_modified_cart_including_ad_hoc_lines()
    {
        await using var app = await TestApp.CreateAsync();
        var items = await CatalogAsync(app, 2);
        var token = await SetupAsync(app, "edit", items: Docs.ItemOut("ADHOC-9", 3, price: 4m, name: "Custom", unspsc: "12345"));
        await WithService(app, s => s.AddToCartAsync(token, items[0].Id));

        var back = await WithService(app, s => s.CheckoutAsync(token));

        var message = (PunchOutOrderMessage)TestApp.Parse(back.Xml).Message!.Payload!;
        Assert.Equal(["ADHOC-9", items[0].SupplierPartId], message.Items.Select(i => i.ItemId.SupplierPartId));
        Assert.Equal(12m + items[0].UnitPrice, message.Header.Total.Amount);
        Assert.True(TestApp.Validate(back.Xml).IsValid);
    }

    [Fact]
    public async Task Order_message_validates_even_for_an_empty_cart_and_without_deployment_mode()
    {
        await using var app = await TestApp.CreateAsync();
        var token = await SetupAsync(app);

        var back = await WithService(app, s => s.CheckoutAsync(token));

        Assert.True(TestApp.Validate(back.Xml).IsValid);
        Assert.Empty(((PunchOutOrderMessage)TestApp.Parse(back.Xml).Message!.Payload!).Items);
        Assert.DoesNotContain(TestApp.Validate(back.Xml).Issues, i => i.Severity == ValidationSeverity.Error);
    }
}
