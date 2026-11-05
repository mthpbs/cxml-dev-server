using CxmlDevServer.Web.Security;

namespace CxmlDevServer.Web.Cli;

/// <summary>
/// The command line utilities of the executable, run instead of the web host:
/// <c>hash-password [password]</c> and <c>healthcheck [url]</c>. They work the same under
/// <c>dotnet CxmlDevServer.Web.dll</c> and <c>docker run &lt;image&gt;</c> (the image has no shell tools such as curl).
/// </summary>
public static class CliCommands
{
    /// <summary>The URL probed by <c>healthcheck</c> when none is given.</summary>
    public const string DefaultHealthUrl = "http://127.0.0.1:8080/health";

    private const string Usage = """
        Usage:
          hash-password [password]   Print a password hash for Admin:PasswordHash (reads the password from stdin if omitted).
          healthcheck [url]          GET the URL (default http://127.0.0.1:8080/health); exit code 0 for 2xx, otherwise 1.
        """;

    /// <summary>
    /// Runs the command named by the first argument. Returns its exit code, or <c>null</c> when the arguments are not
    /// a command and the web host should start.
    /// </summary>
    public static async Task<int?> TryRunAsync(
        string[] args, TextReader stdin, TextWriter stdout, TextWriter stderr, HttpMessageHandler? healthHandler = null,
        CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
        {
            return null;
        }

        switch (args[0])
        {
            case "hash-password":
                return HashPassword(args, stdin, stdout, stderr);
            case "healthcheck":
                return await HealthCheckAsync(args, stdout, stderr, healthHandler, cancellationToken);
            case "-h" or "--help" or "help":
                await stdout.WriteLineAsync(Usage);
                return 0;
            default:
                return null;
        }
    }

    private static int HashPassword(string[] args, TextReader stdin, TextWriter stdout, TextWriter stderr)
    {
        var password = args.Length > 1 ? args[1] : ReadPassword(stdin, stderr);
        if (string.IsNullOrEmpty(password))
        {
            stderr.WriteLine("No password given. Pass it as an argument or pipe it to stdin.");
            return 2;
        }

        stdout.WriteLine(AdminCredentialValidator.Hash(password));
        return 0;
    }

    private static string? ReadPassword(TextReader stdin, TextWriter stderr)
    {
        if (!Console.IsInputRedirected && ReferenceEquals(stdin, Console.In))
        {
            // Interactive terminal: prompt on stderr (stdout carries only the hash) and do not echo.
            stderr.Write("Password: ");
            var typed = new System.Text.StringBuilder();
            while (Console.ReadKey(intercept: true) is var key && key.Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (typed.Length > 0)
                    {
                        typed.Length--;
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    typed.Append(key.KeyChar);
                }
            }

            stderr.WriteLine();
            return typed.ToString();
        }

        // Piped input: everything up to the final line break is the password.
        return stdin.ReadToEnd().TrimEnd('\r', '\n');
    }

    private static async Task<int> HealthCheckAsync(
        string[] args, TextWriter stdout, TextWriter stderr, HttpMessageHandler? handler, CancellationToken cancellationToken)
    {
        var url = args.Length > 1 ? args[1] : DefaultHealthUrl;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            await stderr.WriteLineAsync($"Not an http(s) URL: {url}");
            return 1;
        }

        using var client = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        client.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await stdout.WriteLineAsync($"healthy ({(int)response.StatusCode})");
                return 0;
            }

            await stderr.WriteLineAsync($"unhealthy: {url} answered {(int)response.StatusCode}");
            return 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            await stderr.WriteLineAsync($"unhealthy: {url} not reachable ({ex.Message})");
            return 1;
        }
    }
}
