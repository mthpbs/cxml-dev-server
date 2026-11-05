using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CxmlDevServer.Infrastructure.Persistence;

/// <summary>Lets the EF tools (<c>dotnet ef</c>) create the context without the web project.</summary>
public sealed class CxmlDevDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CxmlDevDbContext>
{
    /// <inheritdoc />
    public CxmlDevDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CxmlDevDbContext>()
            .UseSqlite("Data Source=data/cxml-dev.db")
            .Options;
        return new CxmlDevDbContext(options);
    }
}
