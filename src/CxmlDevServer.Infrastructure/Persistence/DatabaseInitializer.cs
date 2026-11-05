using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Infrastructure.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CxmlDevServer.Infrastructure.Persistence;

/// <summary>Creates the data directory, applies migrations and seeds demo data into an empty database.</summary>
public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly CxmlDevDbContext _db;
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>Creates the initializer.</summary>
    public DatabaseInitializer(CxmlDevDbContext db, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var fileDatabase = EnsureDataDirectory(_db.Database.GetConnectionString());
        await _db.Database.MigrateAsync(cancellationToken);
        if (fileDatabase)
        {
            // WAL lets the web UI read while documents are being processed.
            await _db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        }

        await SeedAsync(cancellationToken);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var empty = !await _db.SupplierIdentities.AnyAsync(cancellationToken)
                    && !await _db.TradingPartners.AnyAsync(cancellationToken)
                    && !await _db.CatalogItems.AnyAsync(cancellationToken);
        if (!empty)
        {
            return;
        }

        _db.SupplierIdentities.AddRange(SeedData.SupplierIdentities());
        _db.TradingPartners.AddRange(SeedData.TradingPartners());
        _db.CatalogItems.AddRange(SeedData.CatalogItems());
        _db.ResponseScenarios.AddRange(SeedData.Scenarios());
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded demo supplier identities, trading partners, catalog items and scenarios.");
    }

    /// <summary>Creates the directory of a file database; returns <c>false</c> for in-memory databases.</summary>
    private static bool EnsureDataDirectory(string? connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Path.GetDirectoryName(Path.GetFullPath(dataSource)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        return true;
    }
}
