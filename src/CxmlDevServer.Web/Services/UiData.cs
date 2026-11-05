using CxmlDevServer.Core.Abstractions;

namespace CxmlDevServer.Web.Services;

/// <summary>
/// Data access for Blazor components. Circuits live for a long time, so components never hold a database context
/// or a scoped service: every operation gets its own short-lived context or service scope.
/// </summary>
public sealed class UiData
{
    private readonly ICxmlDbContextFactory _contexts;
    private readonly IServiceScopeFactory _scopes;

    /// <summary>Creates the helper.</summary>
    public UiData(ICxmlDbContextFactory contexts, IServiceScopeFactory scopes)
    {
        _contexts = contexts;
        _scopes = scopes;
    }

    /// <summary>Runs a query with a new context.</summary>
    public async Task<T> QueryAsync<T>(Func<ICxmlDbContext, Task<T>> work)
    {
        await using var db = _contexts.CreateDbContext();
        return await work(db);
    }

    /// <summary>Runs a command with a new context.</summary>
    public async Task CommandAsync(Func<ICxmlDbContext, Task> work)
    {
        await using var db = _contexts.CreateDbContext();
        await work(db);
    }

    /// <summary>Runs work that needs scoped application services.</summary>
    public async Task<T> ScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using var scope = _scopes.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    /// <summary>Runs work that needs scoped application services.</summary>
    public async Task ScopeAsync(Func<IServiceProvider, Task> work)
    {
        await using var scope = _scopes.CreateAsyncScope();
        await work(scope.ServiceProvider);
    }
}
