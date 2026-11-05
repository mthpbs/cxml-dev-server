using Microsoft.Data.Sqlite;

namespace CxmlDevServer.Web.Services;

/// <summary>Resolves the SQLite connection string and makes sure the database folder exists.</summary>
internal static class SqliteLocation
{
    private const string DefaultConnectionString = "Data Source=data/cxml-dev.db";

    /// <summary>
    /// Returns the connection string <c>ConnectionStrings:Default</c>; a relative database file is placed below the
    /// content root and its folder is created.
    /// </summary>
    public static string Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var builder = new SqliteConnectionStringBuilder(configuration.GetConnectionString("Default") ?? DefaultConnectionString);
        var file = builder.DataSource;
        if (string.IsNullOrWhiteSpace(file) || file == ":memory:" || builder.Mode == SqliteOpenMode.Memory || file.StartsWith("file:", StringComparison.Ordinal))
        {
            return builder.ConnectionString;
        }

        if (!Path.IsPathRooted(file))
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(environment.ContentRootPath, file));
        }

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ConnectionString;
    }
}
