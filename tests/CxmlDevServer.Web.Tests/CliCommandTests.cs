using System.Net;
using System.Net.Sockets;
using CxmlDevServer.Web.Cli;
using CxmlDevServer.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;

namespace CxmlDevServer.Web.Tests;

public sealed class CliCommandTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public CliCommandTests(WebAppFactory factory) => _factory = factory;

    private static async Task<(int? Exit, string Out, string Err)> RunAsync(string[] args, string stdin = "", HttpMessageHandler? handler = null)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = await CliCommands.TryRunAsync(args, new StringReader(stdin), stdout, stderr, handler);
        return (exit, stdout.ToString(), stderr.ToString());
    }

    [Fact]
    public async Task Other_arguments_are_left_to_the_web_host()
    {
        Assert.Null((await RunAsync([])).Exit);
        Assert.Null((await RunAsync(["--urls", "http://0.0.0.0:8080"])).Exit);
        Assert.Null((await RunAsync(["--environment", "Development"])).Exit);
    }

    [Fact]
    public async Task Hash_password_prints_only_a_hash_that_the_password_hasher_accepts()
    {
        var (exit, output, error) = await RunAsync(["hash-password", "s3cret pass"]);

        Assert.Equal(0, exit);
        Assert.Equal("", error);
        var hash = output.Trim();
        Assert.DoesNotContain("\n", hash);
        var hasher = new PasswordHasher<string>();
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword("admin", hash, "s3cret pass"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword("admin", hash, "other"));
    }

    [Fact]
    public async Task Hash_password_reads_the_password_from_stdin_without_the_trailing_newline()
    {
        var (exit, output, _) = await RunAsync(["hash-password"], "piped-pw\r\n");

        Assert.Equal(0, exit);
        Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<string>().VerifyHashedPassword("admin", output.Trim(), "piped-pw"));
    }

    [Fact]
    public async Task Hash_password_without_any_password_fails()
    {
        var (exit, output, error) = await RunAsync(["hash-password"], "");

        Assert.Equal(2, exit);
        Assert.Equal("", output);
        Assert.Contains("No password", error);
    }

    [Fact]
    public async Task The_hash_from_the_command_is_accepted_by_the_login()
    {
        var (_, output, _) = await RunAsync(["hash-password", "from-the-cli"]);
        using var factory = _factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Admin:Enabled", "true");
            b.UseSetting("Admin:PasswordHash", output.Trim());
        });
        var browser = AdminWebAppFactory.WithOwnAddress(factory);
        var token = Http.ExtractAntiforgeryToken(await browser.GetStringAsync("/login"));

        var login = await browser.PostAsync("/account/login", new FormUrlEncodedContent(
            [new("username", "admin"), new("password", "from-the-cli"), new("__RequestVerificationToken", token)]));

        Assert.Equal("/", login.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/partners")).StatusCode);
    }

    [Fact]
    public async Task Healthcheck_succeeds_against_a_healthy_server()
    {
        var (exit, output, _) = await RunAsync(["healthcheck", "http://localhost/health"], handler: _factory.Server.CreateHandler());

        Assert.Equal(0, exit);
        Assert.Contains("healthy", output);
    }

    [Fact]
    public async Task Healthcheck_fails_for_a_non_success_status()
    {
        var (exit, _, error) = await RunAsync(["healthcheck", "http://localhost/does-not-exist"], handler: _factory.Server.CreateHandler());

        Assert.Equal(1, exit);
        Assert.Contains("unhealthy", error);
    }

    [Fact]
    public async Task Healthcheck_talks_real_http_to_a_listener_and_fails_when_nothing_listens()
    {
        var port = FreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        var serving = Task.Run(async () =>
        {
            for (var i = 0; i < 2; i++)
            {
                var context = await listener.GetContextAsync();
                context.Response.StatusCode = context.Request.Url!.AbsolutePath == "/health" ? 200 : 503;
                context.Response.Close();
            }
        });

        Assert.Equal(0, (await RunAsync(["healthcheck", $"http://127.0.0.1:{port}/health"])).Exit);
        Assert.Equal(1, (await RunAsync(["healthcheck", $"http://127.0.0.1:{port}/broken"])).Exit);
        await serving;
        listener.Stop();

        var closed = await RunAsync(["healthcheck", $"http://127.0.0.1:{port}/health"]);
        Assert.Equal(1, closed.Exit);
        Assert.Contains("not reachable", closed.Err);
    }

    [Fact]
    public async Task Healthcheck_rejects_an_url_that_is_not_http()
    {
        Assert.Equal(1, (await RunAsync(["healthcheck", "ftp://example.com/"])).Exit);
        Assert.Equal(1, (await RunAsync(["healthcheck", "not a url"])).Exit);
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
