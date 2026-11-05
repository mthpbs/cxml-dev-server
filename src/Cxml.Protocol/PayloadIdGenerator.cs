using System.Security.Cryptography;

namespace Cxml.Protocol;

/// <summary>
/// Generates cXML payload identifiers of the form <c>{yyyyMMddHHmmssfff}.{pid}.{random}@{host}</c>.
/// </summary>
public static class PayloadIdGenerator
{
    /// <summary>Generates a new payload id using the current UTC time and machine name.</summary>
    public static string Generate() => Generate(DateTimeOffset.UtcNow, Environment.MachineName);

    /// <summary>Generates a payload id for an explicit time and host (useful for tests).</summary>
    /// <param name="now">Time embedded in the id (rendered in UTC).</param>
    /// <param name="host">Host name appended after the <c>@</c>.</param>
    public static string Generate(DateTimeOffset now, string host)
    {
        var random = RandomNumberGenerator.GetInt32(100_000_000, int.MaxValue);
        var safeHost = string.IsNullOrWhiteSpace(host) ? "localhost" : host.Trim().ToLowerInvariant().Replace(' ', '-');
        return $"{now.UtcDateTime:yyyyMMddHHmmssfff}.{Environment.ProcessId}.{random}@{safeHost}";
    }
}
