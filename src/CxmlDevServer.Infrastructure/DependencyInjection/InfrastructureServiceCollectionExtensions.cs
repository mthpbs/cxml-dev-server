using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Messaging;
using CxmlDevServer.Infrastructure.Outbound;
using CxmlDevServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CxmlDevServer.Infrastructure.DependencyInjection;

/// <summary>Registers the persistence and outbound HTTP services.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>The connection string used when <c>ConnectionStrings:Default</c> is not configured.</summary>
    public const string DefaultConnectionString = "Data Source=data/cxml-dev.db";

    /// <summary>
    /// Registers the SQLite database (connection string <c>ConnectionStrings:Default</c>, default
    /// <see cref="DefaultConnectionString"/>), the initializer (run at startup by a hosted service) and the outbound client.
    /// </summary>
    public static IServiceCollection AddCxmlDevServerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString("Default") ?? DefaultConnectionString;
        return services.AddCxmlDevServerInfrastructure((_, options) => options.UseSqlite(connectionString));
    }

    /// <summary>Registers the persistence services with a custom database configuration (for example an open in-memory connection in tests).</summary>
    public static IServiceCollection AddCxmlDevServerInfrastructure(
        this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> configureDatabase)
    {
        ArgumentNullException.ThrowIfNull(configureDatabase);

        // A singleton factory serves Blazor components; the scoped context (used by request-scoped services)
        // is created from it so both share one configuration.
        services.AddDbContextFactory<CxmlDevDbContext>(configureDatabase);
        services.TryAddScoped(sp => sp.GetRequiredService<IDbContextFactory<CxmlDevDbContext>>().CreateDbContext());
        services.TryAddScoped<ICxmlDbContext>(sp => sp.GetRequiredService<CxmlDevDbContext>());
        services.TryAddSingleton<ICxmlDbContextFactory, CxmlDbContextFactoryAdapter>();

        services.TryAddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddHostedService<DatabaseInitializerHostedService>();

        services.AddHttpClient(CxmlOutboundClient.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(100));
        services.TryAddScoped<ICxmlOutboundClient, CxmlOutboundClient>();
        return services;
    }
}
