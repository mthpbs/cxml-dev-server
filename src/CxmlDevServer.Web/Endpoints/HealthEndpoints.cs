namespace CxmlDevServer.Web.Endpoints;

/// <summary>A trivial liveness probe.</summary>
public static class HealthEndpoints
{
    /// <summary>Maps <c>GET /health</c>.</summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Text("OK", "text/plain")).ExcludeFromDescription();
        return app;
    }
}
