using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Infrastructure.Persistence;

/// <summary>Runs <see cref="IDatabaseInitializer"/> when the host starts (unless disabled in the options).</summary>
public sealed class DatabaseInitializerHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly CxmlServerOptions _options;

    /// <summary>Creates the hosted service.</summary>
    public DatabaseInitializerHostedService(IServiceScopeFactory scopes, IOptions<CxmlServerOptions> options)
    {
        _scopes = scopes;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.InitializeDatabaseOnStartup)
        {
            return;
        }

        await using var scope = _scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
