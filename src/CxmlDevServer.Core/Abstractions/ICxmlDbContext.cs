using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CxmlDevServer.Core.Abstractions;

/// <summary>The persistence surface Core needs; implemented by the EF Core context in the infrastructure project.</summary>
public interface ICxmlDbContext : IDisposable, IAsyncDisposable
{
    /// <summary>Trading partners.</summary>
    DbSet<TradingPartner> TradingPartners { get; }

    /// <summary>Supplier identities.</summary>
    DbSet<SupplierIdentity> SupplierIdentities { get; }

    /// <summary>Catalog items.</summary>
    DbSet<CatalogItem> CatalogItems { get; }

    /// <summary>PunchOut sessions.</summary>
    DbSet<PunchOutSession> PunchOutSessions { get; }

    /// <summary>PunchOut cart lines.</summary>
    DbSet<PunchOutCartLine> PunchOutCartLines { get; }

    /// <summary>Purchase orders.</summary>
    DbSet<PurchaseOrder> PurchaseOrders { get; }

    /// <summary>Purchase order lines.</summary>
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }

    /// <summary>Message log.</summary>
    DbSet<CxmlMessageLog> MessageLogs { get; }

    /// <summary>Validation issues of logged messages.</summary>
    DbSet<ValidationIssueRecord> ValidationIssues { get; }

    /// <summary>Response scenarios.</summary>
    DbSet<ResponseScenario> ResponseScenarios { get; }

    /// <summary>Database operations (transactions, raw SQL).</summary>
    DatabaseFacade Database { get; }

    /// <summary>The change tracker.</summary>
    ChangeTracker ChangeTracker { get; }

    /// <summary>Persists all pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
