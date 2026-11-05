using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Handlers;
using CxmlDevServer.Core.Orders;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Tests;

public class OrderRequestTests
{
    private static Task<PurchaseOrder> LoadOrderAsync(TestApp app, string orderId) =>
        app.QueryAsync(db => db.PurchaseOrders.AsNoTracking().Include(o => o.Lines).SingleAsync(o => o.OrderId == orderId));

    [Fact]
    public async Task New_order_is_stored_with_header_and_lines()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(Docs.OrderRequest("PO-100", lines: [("A-1", 3, "10.00"), ("A-2", 1, "5.50")], total: "35.50"));

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var order = await LoadOrderAsync(app, "PO-100");
        Assert.Equal(1, order.Version);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.Equal(35.50m, order.TotalAmount);
        Assert.Equal("test", order.DeploymentMode);
        Assert.NotNull(order.OrderDate);
        Assert.NotNull(order.BillToJson);
        Assert.Equal(["A-1", "A-2"], order.Lines.OrderBy(l => l.LineNumber).Select(l => l.SupplierPartId));
        Assert.Equal(3, order.Lines.Single(l => l.SupplierPartId == "A-1").Quantity);
        Assert.Equal("UNSPSC", order.Lines[0].ClassificationDomain);
        Assert.Equal("EA", order.Lines[0].UnitOfMeasure);
        Assert.NotNull(order.MessageLogId);
    }

    [Fact]
    public async Task Update_applies_a_new_version_and_replaces_the_lines()
    {
        await using var app = await TestApp.CreateAsync();
        await app.PostAsync(Docs.OrderRequest("PO-200", version: 1));

        var update = await app.PostAsync(Docs.OrderRequest("PO-200", "update", version: 2, total: "9.00", lines: [("B-1", 3, "3.00")]));

        Assert.Equal(200, TestApp.StatusOf(update).Code);
        var order = await LoadOrderAsync(app, "PO-200");
        Assert.Equal(2, order.Version);
        Assert.Equal(PurchaseOrderStatus.Updated, order.Status);
        Assert.Equal(9.00m, order.TotalAmount);
        Assert.Equal("B-1", Assert.Single(order.Lines).SupplierPartId);
        Assert.Equal(1, await app.QueryAsync(db => db.PurchaseOrders.CountAsync()));
        Assert.Equal(1, await app.QueryAsync(db => db.PurchaseOrderLines.CountAsync()));

        // Both requests share the order's correlation id.
        var partnerId = (await app.GetPartnerAsync("Demo Buyer")).Id;
        var correlated = await app.QueryAsync(db => db.MessageLogs
            .Where(l => l.CorrelationId == CorrelationIds.ForOrder(partnerId, "PO-200")).CountAsync());
        Assert.Equal(2, correlated);
    }

    [Fact]
    public async Task Update_without_order_version_counts_up()
    {
        await using var app = await TestApp.CreateAsync();
        await app.PostAsync(Docs.OrderRequest("PO-201"));
        await app.PostAsync(Docs.OrderRequest("PO-201", "update"));

        Assert.Equal(2, (await LoadOrderAsync(app, "PO-201")).Version);
    }

    [Fact]
    public async Task Delete_cancels_the_order()
    {
        await using var app = await TestApp.CreateAsync();
        await app.PostAsync(Docs.OrderRequest("PO-300"));

        var result = await app.PostAsync(Docs.OrderRequest("PO-300", "delete", version: 2));

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var order = await LoadOrderAsync(app, "PO-300");
        Assert.Equal(PurchaseOrderStatus.Cancelled, order.Status);
        Assert.Equal(2, order.Version);
        Assert.Equal(2, order.Lines.Count);
    }

    [Fact]
    public async Task Duplicate_payload_id_is_acknowledged_without_creating_a_second_order()
    {
        await using var app = await TestApp.CreateAsync();
        const string payloadId = "20240501.1.1@buyer";
        await app.PostAsync(Docs.OrderRequest("PO-400", payloadId: payloadId));

        var duplicate = await app.PostAsync(Docs.OrderRequest("PO-400", payloadId: payloadId));

        Assert.Equal(200, TestApp.StatusOf(duplicate).Code);
        Assert.Equal(1, await app.QueryAsync(db => db.PurchaseOrders.CountAsync()));
        Assert.Equal(2, await app.QueryAsync(db => db.PurchaseOrderLines.CountAsync()));
        var log = await app.GetLogAsync(duplicate.LogId!.Value);
        Assert.Contains(log.Issues, i => i.Code == OrderRequestHandler.DuplicatePayloadCode && i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public async Task Same_payload_id_after_a_failed_attempt_is_processed()
    {
        await using var app = await TestApp.CreateAsync();
        const string payloadId = "retry-1@buyer";
        await app.ModifyAsync(async db => db.ResponseScenarios.Add(new ResponseScenario
        {
            Name = "fail once", StatusCode = 500, RemainingUses = 1, DocumentType = "OrderRequest",
        }));

        Assert.Equal(500, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-RETRY", payloadId: payloadId))).Code);
        Assert.Empty(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));

        var retry = await app.PostAsync(Docs.OrderRequest("PO-RETRY", payloadId: payloadId));
        Assert.Equal(200, TestApp.StatusOf(retry).Code);
        Assert.Single(await app.QueryAsync(db => db.PurchaseOrders.ToListAsync()));
        Assert.DoesNotContain((await app.GetLogAsync(retry.LogId!.Value)).Issues, i => i.Code == OrderRequestHandler.DuplicatePayloadCode);
    }

    [Fact]
    public async Task Update_and_delete_of_unknown_orders_warn_in_lenient_mode()
    {
        await using var app = await TestApp.CreateAsync();

        var update = await app.PostAsync(Docs.OrderRequest("PO-U", "update"));
        Assert.Equal(200, TestApp.StatusOf(update).Code);
        Assert.Contains((await app.GetLogAsync(update.LogId!.Value)).Issues, i => i.Code == OrderRequestHandler.UnknownOrderCode);
        Assert.Equal(PurchaseOrderStatus.Updated, (await LoadOrderAsync(app, "PO-U")).Status);

        var delete = await app.PostAsync(Docs.OrderRequest("PO-D", "delete"));
        Assert.Equal(200, TestApp.StatusOf(delete).Code);
        Assert.Contains((await app.GetLogAsync(delete.LogId!.Value)).Issues, i => i.Code == OrderRequestHandler.UnknownOrderCode);
        Assert.Equal(1, await app.QueryAsync(db => db.PurchaseOrders.CountAsync()));
    }

    [Fact]
    public async Task Strict_mode_rejects_unknown_updates_and_deletes_and_existing_new_orders()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SetPartnerAsync("Demo Buyer", p => p.ValidationMode = ValidationMode.Strict);

        Assert.Equal(412, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-S1", "update"))).Code);
        Assert.Equal(412, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-S1", "delete"))).Code);

        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-S2"))).Code);
        Assert.Equal(409, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-S2"))).Code);
        Assert.Equal(409, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-S2", "update", version: 1))).Code);
        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-S2", "update", version: 2))).Code);
    }

    [Fact]
    public async Task Same_order_id_from_different_partners_creates_separate_orders()
    {
        await using var app = await TestApp.CreateAsync();
        var acme = Docs.Envelope(
            Docs.Request(Docs.OrderRequestPayload("PO-SAME")),
            senderDomain: "AribaNetworkUserId", senderIdentity: "sysadmin@ariba.com", secret: "abracadabra",
            fromDomain: "AribaNetworkUserId", fromIdentity: "admin@acme.com", toIdentity: "942888711");

        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(Docs.OrderRequest("PO-SAME"))).Code);
        Assert.Equal(200, TestApp.StatusOf(await app.PostAsync(acme)).Code);

        Assert.Equal(2, await app.QueryAsync(db => db.PurchaseOrders.CountAsync(o => o.OrderId == "PO-SAME")));
    }

    [Fact]
    public async Task Rule_warnings_are_logged_for_inconsistent_totals_and_bad_codes()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.OrderRequest("PO-W", total: "99.99").Replace("isoCountryCode=\"US\"", "isoCountryCode=\"XX\"");

        var result = await app.PostAsync(xml);

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var issues = (await app.GetLogAsync(result.LogId!.Value)).Issues;
        Assert.Contains(issues, i => i.Code == "RULE-ORDER-TOTAL" && i.Severity == ValidationSeverity.Warning);
        Assert.Contains(issues, i => i.Code == "RULE-COUNTRY-CODE" && i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public async Task Duplicate_line_numbers_are_reported_as_errors()
    {
        await using var app = await TestApp.CreateAsync();
        var xml = Docs.OrderRequest("PO-L").Replace("lineNumber=\"2\"", "lineNumber=\"1\"");

        var result = await app.PostAsync(xml);

        Assert.Contains((await app.GetLogAsync(result.LogId!.Value)).Issues,
            i => i.Code == "RULE-LINENUMBERS-UNIQUE" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public async Task Deployment_mode_mismatch_warns()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SetPartnerAsync("Demo Buyer", p => p.DeploymentMode = "production");

        var result = await app.PostAsync(Docs.OrderRequest("PO-M"));

        Assert.Contains((await app.GetLogAsync(result.LogId!.Value)).Issues, i => i.Code == "RULE-DEPLOYMENT-MODE");
    }
}
