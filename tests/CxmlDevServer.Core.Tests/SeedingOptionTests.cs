using CxmlDevServer.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CxmlDevServer.Core.Tests.Support;

namespace CxmlDevServer.Core.Tests;

public sealed class SeedingOptionTests
{
    [Fact]
    public async Task Without_demo_partners_only_identities_and_catalog_are_seeded()
    {
        await using var app = await TestApp.CreateAsync(o => o.SeedDemoPartners = false);

        Assert.Equal(0, await app.QueryAsync(db => db.TradingPartners.CountAsync()));
        Assert.Equal(0, await app.QueryAsync(db => db.ResponseScenarios.CountAsync()));
        Assert.Equal(2, await app.QueryAsync(db => db.SupplierIdentities.CountAsync()));
        Assert.True(await app.QueryAsync(db => db.CatalogItems.CountAsync()) > 20);
    }

    [Fact]
    public async Task Demo_partners_are_seeded_by_default()
    {
        await using var app = await TestApp.CreateAsync();

        Assert.Equal(2, await app.QueryAsync(db => db.TradingPartners.CountAsync()));
        Assert.Equal(1, await app.QueryAsync(db => db.ResponseScenarios.CountAsync()));
    }

    [Fact]
    public async Task Initializing_again_does_not_add_demo_partners_to_a_database_that_was_seeded_without_them()
    {
        await using var app = await TestApp.CreateAsync(o => o.SeedDemoPartners = false);

        await app.ScopeAsync(async sp =>
        {
            await sp.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
            return 0;
        });

        Assert.Equal(0, await app.QueryAsync(db => db.TradingPartners.CountAsync()));
    }
}
