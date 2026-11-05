using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CxmlDevServer.Web.Tests.Support;

/// <summary>The application with the admin login switched on (user <c>admin</c>, password <see cref="Password"/>, hashed).</summary>
public sealed class AdminWebAppFactory : WebAppFactory
{
    public const string Username = "admin";
    public const string Password = "secret123";

    private static int _clientCounter;

    public static string PasswordHash { get; } = new PasswordHasher<string>().HashPassword(string.Empty, Password);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Admin:Enabled", "true");
        builder.UseSetting("Admin:Username", Username);
        builder.UseSetting("Admin:PasswordHash", PasswordHash);
        // Forwarded headers let every test client pose as its own address, so the login rate limit (per address)
        // of one test never affects another.
        builder.UseSetting("FORWARDEDHEADERS_ENABLED", "true");
    }

    /// <summary>A cookie-keeping, non-redirecting client with an address of its own (see above).</summary>
    public HttpClient CreateBrowserWithOwnAddress() => WithOwnAddress(this);

    public static HttpClient WithOwnAddress(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var n = Interlocked.Increment(ref _clientCounter);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.{n / 65536 % 256}.{n / 256 % 256}.{n % 256}");
        return client;
    }
}
