using CxmlDevServer.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Infrastructure.Persistence;

/// <summary>Adapts EF's <see cref="IDbContextFactory{TContext}"/> to Core's <see cref="ICxmlDbContextFactory"/>.</summary>
public sealed class CxmlDbContextFactoryAdapter : ICxmlDbContextFactory
{
    private readonly IDbContextFactory<CxmlDevDbContext> _factory;

    /// <summary>Creates the adapter.</summary>
    public CxmlDbContextFactoryAdapter(IDbContextFactory<CxmlDevDbContext> factory) => _factory = factory;

    /// <inheritdoc />
    public ICxmlDbContext CreateDbContext() => _factory.CreateDbContext();
}
