using Cxml.Protocol;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Parsing;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.DependencyInjection;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Options;
using CxmlDevServer.Core.Pipeline;
using CxmlDevServer.Infrastructure.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CxmlDevServer.Core.Tests.Support;

/// <summary>A complete Core + Infrastructure stack on an in-memory SQLite database, seeded like a fresh install.</summary>
public sealed class TestApp : IAsyncDisposable
{
    public const string BaseUrl = "http://localhost:5050";

    private static readonly CxmlDtdValidator Validator = new();
    private static readonly CxmlReader Reader = new();

    private readonly SqliteConnection? _connection;
    private readonly string? _databaseFile;

    private TestApp(SqliteConnection? connection, string? databaseFile, ServiceProvider services)
    {
        _connection = connection;
        _databaseFile = databaseFile;
        Services = services;
    }

    public ServiceProvider Services { get; }

    public static async Task<TestApp> CreateAsync(
        Action<CxmlServerOptions>? configureOptions = null,
        Action<IServiceCollection>? configureServices = null,
        bool fileDatabase = false)
    {
        // In-memory databases live on one open connection; a file database lets requests run truly concurrently.
        SqliteConnection? connection = null;
        string? file = null;
        if (fileDatabase)
        {
            file = Path.Combine(Path.GetTempPath(), $"cxml-test-{Guid.NewGuid():N}.db");
        }
        else
        {
            connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
        }

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddCxmlDevServerCore(new ConfigurationBuilder().Build());
        services.Configure<CxmlServerOptions>(o =>
        {
            o.PublicBaseUrl = null;
            configureOptions?.Invoke(o);
        });
        services.AddCxmlDevServerInfrastructure((_, o) =>
        {
            if (connection is not null)
            {
                o.UseSqlite(connection);
            }
            else
            {
                o.UseSqlite($"Data Source={file}");
            }
        });
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider(validateScopes: true);
        var app = new TestApp(connection, file, provider);
        await app.ScopeAsync(async sp =>
        {
            await sp.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
            return 0;
        });
        return app;
    }

    /// <summary>Runs <paramref name="action"/> in a fresh scope (like one HTTP request).</summary>
    public async Task<T> ScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>Runs a query against a fresh database context.</summary>
    public Task<T> QueryAsync<T>(Func<ICxmlDbContext, Task<T>> query) =>
        ScopeAsync(sp => query(sp.GetRequiredService<ICxmlDbContext>()));

    /// <summary>Changes data directly in the database.</summary>
    public async Task ModifyAsync(Func<ICxmlDbContext, Task> change)
    {
        await ScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ICxmlDbContext>();
            await change(db);
            await db.SaveChangesAsync();
            return 0;
        });
    }

    public Task SetPartnerAsync(string name, Action<TradingPartner> change) =>
        ModifyAsync(async db => change(await db.TradingPartners.SingleAsync(p => p.Name == name)));

    public Task<TradingPartner> GetPartnerAsync(string name) =>
        QueryAsync(db => db.TradingPartners.AsNoTracking().SingleAsync(p => p.Name == name));

    /// <summary>
    /// Posts a document through the inbound pipeline. Unless <paramref name="validateResponse"/> is false the
    /// response body is asserted to be DTD-valid cXML.
    /// </summary>
    public async Task<CxmlInboundResult> PostAsync(
        string xml,
        string? contentType = "text/xml",
        bool validateResponse = true,
        IReadOnlyCollection<KeyValuePair<string, string?>>? formFields = null)
    {
        var result = await ScopeAsync(sp => sp.GetRequiredService<CxmlInboundProcessor>().ProcessAsync(new CxmlInboundRequest
        {
            Body = System.Text.Encoding.UTF8.GetBytes(xml),
            ContentType = contentType,
            RemoteEndpoint = "127.0.0.1:12345",
            BaseUrl = BaseUrl,
            FormFields = formFields,
        }));

        if (validateResponse && !result.Abort)
        {
            var validation = Validator.Validate(result.ResponseText);
            Assert.True(validation.IsValid, "Response is not DTD-valid:\n" + string.Join('\n', validation.Issues) + "\n" + result.ResponseText);
            Assert.StartsWith("<?xml", result.ResponseText);
        }

        return result;
    }

    /// <summary>The cXML status of a response.</summary>
    public static Status StatusOf(CxmlInboundResult result) =>
        Reader.Read(result.ResponseText).Response?.Status ?? throw new InvalidOperationException("Not a Response: " + result.ResponseText);

    public static CxmlEnvelope Parse(string xml) => Reader.Read(xml);

    public static ValidationResult Validate(string xml) => Validator.Validate(xml);

    public Task<CxmlMessageLog> GetLogAsync(int logId) =>
        QueryAsync(db => db.MessageLogs.AsNoTracking().Include(l => l.Issues).SingleAsync(l => l.Id == logId));

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        if (_databaseFile is not null)
        {
            SqliteConnection.ClearAllPools();
            foreach (var path in Directory.GetFiles(Path.GetDirectoryName(_databaseFile)!, Path.GetFileName(_databaseFile) + "*"))
            {
                File.Delete(path);
            }
        }
    }
}
