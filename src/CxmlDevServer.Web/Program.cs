using Microsoft.EntityFrameworkCore;
using CxmlDevServer.Core.DependencyInjection;
using CxmlDevServer.Infrastructure.DependencyInjection;
using CxmlDevServer.Web.Components;
using CxmlDevServer.Web.Endpoints;
using CxmlDevServer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient();
builder.Services.AddLogging();

builder.Services.AddCxmlDevServerCore(builder.Configuration);
// The database location is resolved when the first context is created, so that the final configuration
// (including overrides from tests and the environment) decides, and "data/" is created below the content root.
builder.Services.AddCxmlDevServerInfrastructure(
    (sp, options) => options.UseSqlite(SqliteLocation.Resolve(
        sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<IHostEnvironment>())));
builder.Services.AddCxmlDevServerWeb();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// A development tool that is also run behind plain HTTP and proxies: no HTTPS redirection or HSTS.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapCxmlEndpoints();
app.MapPunchOutEndpoints();
app.MapTestBuyerEndpoints();
app.MapHealthEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

/// <summary>The application entry point; public so that integration tests can host it.</summary>
public partial class Program;
