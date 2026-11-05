using CxmlDevServer.Core.DependencyInjection;
using CxmlDevServer.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Tests.Support;

public static class TestServiceExtensions
{
    /// <summary>Registers Core and Infrastructure on a file database configured through <c>ConnectionStrings:Default</c>.</summary>
    public static IServiceCollection AddCxmlOnFileDatabase(this IServiceCollection services, string path)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = $"Data Source={path}" })
            .Build();
        return services.AddCxmlDevServerCore(configuration).AddCxmlDevServerInfrastructure(configuration);
    }
}
