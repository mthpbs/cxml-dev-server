using Microsoft.EntityFrameworkCore;
using CxmlDevServer.Core.DependencyInjection;
using CxmlDevServer.Infrastructure.DependencyInjection;
using CxmlDevServer.Web.Cli;
using CxmlDevServer.Web.Components;
using CxmlDevServer.Web.Endpoints;
using CxmlDevServer.Web.Security;
using CxmlDevServer.Web.Services;

// `hash-password` and `healthcheck` are small command line tools in the same executable (the container image has
// no shell utilities); they run instead of the web host.
if (await CliCommands.TryRunAsync(args, Console.In, Console.Out, Console.Error) is { } exitCode)
{
    return exitCode;
}

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
builder.Services.AddCxmlDataProtection();
builder.Services.AddCxmlAdminAuth(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// A development tool that is also run behind plain HTTP and proxies: no HTTPS redirection or HSTS.
// Forwarded headers (X-Forwarded-For/-Proto) are enabled with ASPNETCORE_FORWARDEDHEADERS_ENABLED=true, which the
// default host turns into the forwarded headers middleware.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

// Buyer-facing endpoints carry no authorization: they must stay open when the admin login is on.
app.MapStaticAssets();
app.MapCxmlEndpoints();
app.MapPunchOutEndpoints();
app.MapTestBuyerEndpoints();
app.MapHealthEndpoints();
app.MapAccountEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
return 0;

/// <summary>The application entry point; public so that integration tests can host it.</summary>
public partial class Program;
