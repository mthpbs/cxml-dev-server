using System.Globalization;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CxmlDevServer.Infrastructure.Persistence;

/// <summary>The EF Core (SQLite) database context of the simulator.</summary>
public class CxmlDevDbContext : DbContext, ICxmlDbContext
{
    /// <summary>Creates the context.</summary>
    public CxmlDevDbContext(DbContextOptions<CxmlDevDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public DbSet<TradingPartner> TradingPartners => Set<TradingPartner>();

    /// <inheritdoc />
    public DbSet<SupplierIdentity> SupplierIdentities => Set<SupplierIdentity>();

    /// <inheritdoc />
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();

    /// <inheritdoc />
    public DbSet<PunchOutSession> PunchOutSessions => Set<PunchOutSession>();

    /// <inheritdoc />
    public DbSet<PunchOutCartLine> PunchOutCartLines => Set<PunchOutCartLine>();

    /// <inheritdoc />
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    /// <inheritdoc />
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    /// <inheritdoc />
    public DbSet<CxmlMessageLog> MessageLogs => Set<CxmlMessageLog>();

    /// <inheritdoc />
    public DbSet<ValidationIssueRecord> ValidationIssues => Set<ValidationIssueRecord>();

    /// <inheritdoc />
    public DbSet<ResponseScenario> ResponseScenarios => Set<ResponseScenario>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CxmlDevDbContext).Assembly);

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no native date or decimal types: store UTC ISO-8601 text (sorts correctly) and doubles
        // (so ORDER BY and SUM work). Money amounts here have at most four decimals, well inside double precision.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToSortableTextConverter>();
        configurationBuilder.Properties<decimal>().HaveConversion<DecimalToDoubleConverter>();
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAuditedEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampAuditedEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampAuditedEntities()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditedEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = now;
                    }

                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}

/// <summary>Stores a <see cref="DateTimeOffset"/> as fixed-width UTC text so that SQLite can order and compare it.</summary>
public sealed class DateTimeOffsetToSortableTextConverter : ValueConverter<DateTimeOffset, string>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    /// <summary>Creates the converter.</summary>
    public DateTimeOffsetToSortableTextConverter()
        : base(
            value => value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture),
            text => new DateTimeOffset(DateTime.SpecifyKind(
                DateTime.ParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None), DateTimeKind.Utc)))
    {
    }
}

/// <summary>Stores a <see cref="decimal"/> as a double so that SQLite can order and sum it.</summary>
public sealed class DecimalToDoubleConverter : ValueConverter<decimal, double>
{
    /// <summary>Creates the converter.</summary>
    public DecimalToDoubleConverter()
        : base(value => (double)value, value => (decimal)value)
    {
    }
}
