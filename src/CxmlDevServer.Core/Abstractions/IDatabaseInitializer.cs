namespace CxmlDevServer.Core.Abstractions;

/// <summary>Prepares the database at startup: creates it, applies migrations and seeds demo data.</summary>
public interface IDatabaseInitializer
{
    /// <summary>Runs the initialization; safe to call repeatedly.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
