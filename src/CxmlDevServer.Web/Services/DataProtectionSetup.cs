using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Web.Services;

/// <summary>
/// Persists the Data Protection keys (login cookie, antiforgery tokens, Blazor circuit state) so that they survive a
/// restart. <c>DataProtection:KeysPath</c> sets the folder; by default it is a <c>keys</c> folder next to the SQLite
/// file. An empty value keeps the keys in memory: every restart then signs everybody out.
/// </summary>
internal static class DataProtectionSetup
{
    /// <summary>The configuration key of the folder.</summary>
    public const string KeysPathKey = "DataProtection:KeysPath";

    /// <summary>The application name; part of the key isolation, fixed so keys stay valid across containers.</summary>
    public const string ApplicationName = "CxmlDevServer";

    /// <summary>Registers Data Protection; the folder is resolved when the key manager is first used.</summary>
    public static IServiceCollection AddCxmlDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection().SetApplicationName(ApplicationName);
        services.AddOptions<KeyManagementOptions>().Configure<IConfiguration, IHostEnvironment, ILoggerFactory>(
            (options, configuration, environment, loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger(typeof(DataProtectionSetup));
                var folder = ResolveKeysFolder(configuration, environment);
                if (folder is null)
                {
                    logger.LogWarning(
                        "Data Protection keys are kept in memory ({Key} is empty or there is no data folder): "
                        + "logins and antiforgery tokens become invalid when the application restarts.", KeysPathKey);
                    options.XmlRepository = new InMemoryXmlRepository();
                    return;
                }

                Directory.CreateDirectory(folder);
                logger.LogInformation("Data Protection keys are stored in {Folder}.", folder);
                options.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(folder), loggerFactory);
            });
        return services;
    }

    /// <summary>The folder for the keys, or null for in-memory keys.</summary>
    internal static string? ResolveKeysFolder(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration[KeysPathKey];
        if (configured is not null)
        {
            return string.IsNullOrWhiteSpace(configured)
                ? null
                : Path.GetFullPath(configured, environment.ContentRootPath);
        }

        var dataDirectory = SqliteLocation.DataDirectory(SqliteLocation.Resolve(configuration, environment));
        return dataDirectory is null ? null : Path.Combine(dataDirectory, "keys");
    }

    private sealed class InMemoryXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            lock (_elements)
            {
                return [.. _elements];
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            lock (_elements)
            {
                _elements.Add(element);
            }
        }
    }
}
