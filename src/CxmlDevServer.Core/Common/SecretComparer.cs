using System.Security.Cryptography;
using System.Text;

namespace CxmlDevServer.Core.Common;

/// <summary>Constant-time secret comparison.</summary>
public static class SecretComparer
{
    /// <summary>Compares two secrets without leaking where they differ or how long they are.</summary>
    public static bool FixedTimeEquals(string? expected, string? actual)
    {
        var a = SHA256.HashData(Encoding.UTF8.GetBytes(expected ?? string.Empty));
        var b = SHA256.HashData(Encoding.UTF8.GetBytes(actual ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
