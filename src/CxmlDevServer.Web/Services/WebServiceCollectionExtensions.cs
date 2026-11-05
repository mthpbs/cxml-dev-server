namespace CxmlDevServer.Web.Services;

/// <summary>Registers the services of the web layer.</summary>
public static class WebServiceCollectionExtensions
{
    /// <summary>Registers the UI helpers.</summary>
    public static IServiceCollection AddCxmlDevServerWeb(this IServiceCollection services)
    {
        services.AddSingleton<UiData>();
        services.AddScoped<UiUrls>();
        return services;
    }
}
