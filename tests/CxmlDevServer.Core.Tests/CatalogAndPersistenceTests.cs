using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Catalog;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Tests;

public class CatalogAndPersistenceTests
{
    private static Task<PagedResult<CatalogItem>> SearchAsync(TestApp app, CatalogQuery query) =>
        app.ScopeAsync(sp => sp.GetRequiredService<CatalogQueryService>().SearchAsync(query));

    [Fact]
    public async Task Fresh_database_is_seeded_with_demo_data()
    {
        await using var app = await TestApp.CreateAsync();

        var identities = await app.QueryAsync(db => db.SupplierIdentities.AsNoTracking().OrderBy(i => i.Id).ToListAsync());
        Assert.Equal(["123456789", "942888711"], identities.Select(i => i.Identity));
        Assert.All(identities, i => Assert.Equal("DUNS", i.Domain));
        Assert.Equal([true, false], identities.Select(i => i.IsDefault));

        var demo = await app.GetPartnerAsync("Demo Buyer");
        Assert.Equal(("NetworkId", "AN01000000001", "devsecret"), (demo.SenderDomain, demo.SenderIdentity, demo.SharedSecret));
        Assert.Equal(("NetworkId", "AN01000000001"), (demo.FromDomain, demo.FromIdentity));
        Assert.Equal(ValidationMode.Lenient, demo.ValidationMode);
        Assert.Equal(PunchOutPostEncoding.Base64, demo.PunchOutPostEncoding);
        Assert.True(demo.IsActive);

        var acme = await app.GetPartnerAsync("Acme (spec example)");
        Assert.Equal(("AribaNetworkUserId", "sysadmin@ariba.com", "abracadabra"), (acme.SenderDomain, acme.SenderIdentity, acme.SharedSecret));
        Assert.Equal(("AribaNetworkUserId", "admin@acme.com"), (acme.FromDomain, acme.FromIdentity));

        var items = await app.QueryAsync(db => db.CatalogItems.AsNoTracking().ToListAsync());
        Assert.InRange(items.Count, 24, 30);
        Assert.Equal(5, items.Select(i => i.Category).Distinct().Count());
        Assert.Equal(items.Count, items.Select(i => i.SupplierPartId).Distinct().Count());
        Assert.All(items, i =>
        {
            Assert.False(string.IsNullOrWhiteSpace(i.Unspsc));
            Assert.Contains(i.UnitOfMeasure, new[] { "EA", "BX", "PK" });
            Assert.True(i.UnitPrice > 0);
            Assert.NotNull(i.ManufacturerName);
            Assert.NotNull(i.LeadTimeDays);
        });

        var scenario = await app.QueryAsync(db => db.ResponseScenarios.AsNoTracking().SingleAsync());
        Assert.False(scenario.Enabled);
    }

    [Fact]
    public async Task Initialization_is_idempotent_and_never_overwrites_existing_data()
    {
        await using var app = await TestApp.CreateAsync();
        await app.ModifyAsync(async db => (await db.TradingPartners.FirstAsync()).Name = "Renamed");

        await app.ScopeAsync(async sp =>
        {
            await sp.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
            return 0;
        });

        Assert.Equal(2, await app.QueryAsync(db => db.TradingPartners.CountAsync()));
        Assert.Equal(2, await app.QueryAsync(db => db.SupplierIdentities.CountAsync()));
        Assert.Contains(await app.QueryAsync(db => db.TradingPartners.Select(p => p.Name).ToListAsync()), n => n == "Renamed");
    }

    [Fact]
    public async Task File_databases_get_their_directory_created_and_are_migrated()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cxml-init-" + Guid.NewGuid().ToString("N"), "nested");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCxmlOnFileDatabase(Path.Combine(directory, "test.db"));
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
            Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<ICxmlDbContext>().TradingPartners.CountAsync());
        }

        Assert.True(File.Exists(Path.Combine(directory, "test.db")));
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(Path.GetDirectoryName(directory)!, recursive: true);
    }

    [Fact]
    public async Task Timestamps_are_stamped_and_sort_correctly_across_offsets()
    {
        await using var app = await TestApp.CreateAsync();
        var baseTime = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        await app.ModifyAsync(db =>
        {
            // 12:30+05:00 is earlier (07:30Z) than 12:00Z although its local clock reads later.
            db.MessageLogs.Add(new CxmlMessageLog { DocumentType = "A", RawXml = "x", CreatedAt = new DateTimeOffset(2024, 5, 1, 12, 30, 0, TimeSpan.FromHours(5)) });
            db.MessageLogs.Add(new CxmlMessageLog { DocumentType = "B", RawXml = "x", CreatedAt = baseTime });
            db.MessageLogs.Add(new CxmlMessageLog { DocumentType = "C", RawXml = "x", CreatedAt = baseTime.AddMilliseconds(1) });
            return Task.CompletedTask;
        });

        var ordered = await app.QueryAsync(db => db.MessageLogs.OrderBy(l => l.CreatedAt).Select(l => l.DocumentType).ToListAsync());
        Assert.Equal(["A", "B", "C"], ordered);

        var recent = await app.QueryAsync(db => db.MessageLogs.Where(l => l.CreatedAt > baseTime).Select(l => l.DocumentType).ToListAsync());
        Assert.Equal(["C"], recent);

        var partner = await app.GetPartnerAsync("Demo Buyer");
        Assert.True(partner.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.True(partner.UpdatedAt >= partner.CreatedAt);
        var roundTripped = (await app.QueryAsync(db => db.MessageLogs.AsNoTracking().FirstAsync(l => l.DocumentType == "A"))).CreatedAt;
        Assert.Equal(new DateTimeOffset(2024, 5, 1, 7, 30, 0, TimeSpan.Zero), roundTripped);
    }

    [Fact]
    public async Task Updates_refresh_updated_at_but_not_created_at()
    {
        await using var app = await TestApp.CreateAsync();
        var before = await app.GetPartnerAsync("Demo Buyer");
        await Task.Delay(20);

        await app.SetPartnerAsync("Demo Buyer", p => p.Notes = "changed");

        var after = await app.GetPartnerAsync("Demo Buyer");
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.True(after.UpdatedAt > before.UpdatedAt);
    }

    [Fact]
    public async Task Money_amounts_round_trip_exactly()
    {
        await using var app = await TestApp.CreateAsync();
        await app.ModifyAsync(db =>
        {
            db.CatalogItems.Add(new CatalogItem { SupplierPartId = "PRICE-1", ShortName = "p", Description = "p", Category = "Test", UnitPrice = 1299.99m });
            db.CatalogItems.Add(new CatalogItem { SupplierPartId = "PRICE-2", ShortName = "q", Description = "q", Category = "Test", UnitPrice = 0.1m + 0.2m });
            return Task.CompletedTask;
        });

        var prices = await app.QueryAsync(db => db.CatalogItems.AsNoTracking().Where(i => i.Category == "Test").OrderBy(i => i.UnitPrice).Select(i => i.UnitPrice).ToListAsync());

        Assert.Equal([0.3m, 1299.99m], prices);
    }

    [Fact]
    public async Task Deleting_a_partner_keeps_its_history()
    {
        await using var app = await TestApp.CreateAsync();
        await app.PostAsync(Docs.OrderRequest("PO-KEEP"));
        await app.ModifyAsync(async db => db.TradingPartners.Remove(await db.TradingPartners.SingleAsync(p => p.Name == "Demo Buyer")));

        var order = await app.QueryAsync(db => db.PurchaseOrders.SingleAsync());
        Assert.Null(order.PartnerId);
        Assert.Equal(1, await app.QueryAsync(db => db.MessageLogs.CountAsync()));
    }

    [Fact]
    public async Task Catalog_search_filters_pages_and_sorts()
    {
        await using var app = await TestApp.CreateAsync();

        var all = await SearchAsync(app, new CatalogQuery(PageSize: 100));
        Assert.Equal(all.Items.Count, all.TotalCount);
        Assert.Equal(all.Items.OrderBy(i => i.ShortName).Select(i => i.Id), all.Items.Select(i => i.Id));

        var monitors = await SearchAsync(app, new CatalogQuery(Category: "Monitors", PageSize: 100));
        Assert.Equal(5, monitors.TotalCount);
        Assert.All(monitors.Items, i => Assert.Equal("Monitors", i.Category));

        // Case-insensitive, matches name, description, manufacturer and part numbers; all words must match.
        Assert.Equal(2, (await SearchAsync(app, new CatalogQuery("probook"))).TotalCount);
        Assert.Equal(1, (await SearchAsync(app, new CatalogQuery("probook 16-inch"))).TotalCount);
        Assert.Equal(1, (await SearchAsync(app, new CatalogQuery("lap-1003"))).TotalCount);
        Assert.Equal(4, (await SearchAsync(app, new CatalogQuery("northwind"))).TotalCount);
        Assert.Equal(0, (await SearchAsync(app, new CatalogQuery("zzz-nothing"))).TotalCount);
        // Wildcard characters are literal.
        Assert.Equal(0, (await SearchAsync(app, new CatalogQuery("%"))).TotalCount);
        Assert.Equal(0, (await SearchAsync(app, new CatalogQuery("_"))).TotalCount);

        var page1 = await SearchAsync(app, new CatalogQuery(PageSize: 10, Page: 1));
        var page3 = await SearchAsync(app, new CatalogQuery(PageSize: 10, Page: 3));
        Assert.Equal(10, page1.Items.Count);
        Assert.True(page1.HasNext);
        Assert.False(page1.HasPrevious);
        Assert.Equal(all.TotalCount - 20, page3.Items.Count);
        Assert.False(page3.HasNext);
        Assert.Equal(3, page1.TotalPages);
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page3.Items.Select(i => i.Id)));

        var cheap = await SearchAsync(app, new CatalogQuery(Sort: CatalogSort.PriceAscending, PageSize: 3));
        Assert.Equal(cheap.Items.OrderBy(i => i.UnitPrice).Select(i => i.Id), cheap.Items.Select(i => i.Id));
        Assert.Equal(all.Items.Min(i => i.UnitPrice), cheap.Items[0].UnitPrice);
        var dear = await SearchAsync(app, new CatalogQuery(Sort: CatalogSort.PriceDescending, PageSize: 3));
        Assert.Equal(all.Items.Max(i => i.UnitPrice), dear.Items[0].UnitPrice);
    }

    [Fact]
    public async Task Catalog_categories_count_active_items_and_inactive_items_are_hidden()
    {
        await using var app = await TestApp.CreateAsync();

        var categories = await app.ScopeAsync(sp => sp.GetRequiredService<CatalogQueryService>().GetCategoriesAsync());
        Assert.Equal(["Accessories", "Furniture", "Laptops", "Monitors", "Office supplies"], categories.Select(c => c.Name));
        Assert.Equal(await app.QueryAsync(db => db.CatalogItems.CountAsync()), categories.Sum(c => c.ItemCount));

        await app.ModifyAsync(async db => (await db.CatalogItems.FirstAsync(i => i.Category == "Furniture")).IsActive = false);
        categories = await app.ScopeAsync(sp => sp.GetRequiredService<CatalogQueryService>().GetCategoriesAsync());
        Assert.Equal(3, categories.Single(c => c.Name == "Furniture").ItemCount);
        Assert.Equal(3, (await SearchAsync(app, new CatalogQuery(Category: "Furniture"))).TotalCount);
        Assert.Equal(4, (await SearchAsync(app, new CatalogQuery(Category: "Furniture", IncludeInactive: true))).TotalCount);
    }

    [Fact]
    public async Task Catalog_item_lookup_by_id()
    {
        await using var app = await TestApp.CreateAsync();

        var item = await app.ScopeAsync(sp => sp.GetRequiredService<CatalogQueryService>().GetByIdAsync(1));
        Assert.Equal("LAP-1001", item!.SupplierPartId);
        Assert.Null(await app.ScopeAsync(sp => sp.GetRequiredService<CatalogQueryService>().GetByIdAsync(99999)));
    }
}
