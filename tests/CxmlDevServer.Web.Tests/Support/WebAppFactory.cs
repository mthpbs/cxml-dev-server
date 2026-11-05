using Cxml.Protocol.Envelope;
using Cxml.Protocol.Parsing;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Templates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Web.Tests.Support;

/// <summary>Hosts the real application against its own temporary SQLite file (one per test class).</summary>
public sealed class WebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseFile = Path.Combine(Path.GetTempPath(), $"cxml-web-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databaseFile}");
        builder.UseSetting("Cxml:PublicBaseUrl", string.Empty);
    }

    /// <summary>A client that keeps cookies (needed for the antiforgery token) and does not follow redirects.</summary>
    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using var scope = Services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    public Task<T> QueryAsync<T>(Func<ICxmlDbContext, Task<T>> work) =>
        WithScopeAsync(sp => work(sp.GetRequiredService<ICxmlDbContext>()));

    /// <summary>Renders a sender template for the seeded "Demo Buyer" partner (so the credentials are accepted).</summary>
    public async Task<string> RenderTemplateAsync(string templateKey, string baseUrl = "http://localhost")
    {
        var partner = await QueryAsync(db => db.TradingPartners.AsNoTracking().FirstAsync(p => p.Name == "Demo Buyer"));
        return await WithScopeAsync(sp => sp.GetRequiredService<IMessageTemplateService>().RenderAsync(templateKey, partner, baseUrl));
    }

    public CxmlEnvelope Read(string xml) => Services.GetRequiredService<CxmlReader>().Read(xml);

    public ValidationResult Validate(string xml) => Services.GetRequiredService<CxmlDtdValidator>().Validate(xml);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var file in new[] { _databaseFile, _databaseFile + "-shm", _databaseFile + "-wal" })
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // A leftover temp file is harmless.
                }
            }
        }
    }
}
