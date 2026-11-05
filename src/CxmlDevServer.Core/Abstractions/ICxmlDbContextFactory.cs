namespace CxmlDevServer.Core.Abstractions;

/// <summary>
/// Creates short-lived contexts for callers that cannot use a request-scoped one (Blazor Server components,
/// background work). The caller disposes the context.
/// </summary>
public interface ICxmlDbContextFactory
{
    /// <summary>Creates a new context.</summary>
    ICxmlDbContext CreateDbContext();
}
