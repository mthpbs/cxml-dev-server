using System.Security.Cryptography;
using System.Text;
using Cxml.Protocol.Envelope;

namespace Cxml.Protocol.Security;

/// <summary>
/// Computes and verifies <c>CredentialMac</c> values (<c>HMAC-SHA1-96</c>, type <c>FromSenderCredentials</c>)
/// as described in "Alternative Authentication Methods" of the cXML Getting Started guide.
/// </summary>
/// <remarks>
/// The MAC is HMAC-SHA1, keyed with the shared secret, over the UTF-8 bytes of the normalised
/// From domain, From identity, Sender domain, Sender identity, creationDate and expirationDate, each
/// followed by a null byte. The left-most 12 bytes are base64 encoded.
/// </remarks>
public static class CredentialMacCalculator
{
    /// <summary>The only supported algorithm name.</summary>
    public const string Algorithm = "HMAC-SHA1-96";

    /// <summary>The only supported MAC type.</summary>
    public const string MacType = "FromSenderCredentials";

    private const int TruncatedLength = 12;

    /// <summary>Domains whose value is case-sensitive and therefore not lower-cased during normalisation.</summary>
    private static readonly HashSet<string> CaseSensitiveDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "AribaNetworkUserId",
    };

    /// <summary>Computes the base64 MAC for the given (not yet normalised) inputs.</summary>
    /// <param name="fromDomain">From credential domain.</param>
    /// <param name="fromIdentity">From credential identity.</param>
    /// <param name="senderDomain">Sender credential domain.</param>
    /// <param name="senderIdentity">Sender credential identity.</param>
    /// <param name="creationDate">The CredentialMac <c>creationDate</c> attribute, verbatim.</param>
    /// <param name="expirationDate">The CredentialMac <c>expirationDate</c> attribute, verbatim.</param>
    /// <param name="sharedSecret">The shared secret (never normalised).</param>
    public static string Compute(
        string fromDomain,
        string fromIdentity,
        string senderDomain,
        string senderIdentity,
        string creationDate,
        string expirationDate,
        string sharedSecret)
    {
        var data = new List<byte>();
        foreach (var value in new[]
        {
            NormalizeDomain(fromDomain),
            NormalizeIdentity(fromIdentity),
            NormalizeDomain(senderDomain),
            NormalizeIdentity(senderIdentity),
            creationDate,
            expirationDate,
        })
        {
            data.AddRange(Encoding.UTF8.GetBytes(value));
            data.Add(0);
        }

        var hash = HMACSHA1.HashData(Encoding.UTF8.GetBytes(sharedSecret), data.ToArray());
        return Convert.ToBase64String(hash, 0, TruncatedLength);
    }

    /// <summary>
    /// Computes the MAC for a header's primary From and Sender credentials and the dates of
    /// <paramref name="mac"/>.
    /// </summary>
    public static string Compute(CxmlHeader header, CredentialMac mac, string sharedSecret)
    {
        var from = header.From.Primary;
        var sender = header.Sender.Primary;
        return Compute(
            from?.Domain ?? string.Empty,
            from?.Identity ?? string.Empty,
            sender?.Domain ?? string.Empty,
            sender?.Identity ?? string.Empty,
            mac.CreationDate,
            mac.ExpirationDate,
            sharedSecret);
    }

    /// <summary>Verifies the <see cref="CredentialMac.Value"/> of <paramref name="mac"/> in constant time.</summary>
    public static bool Verify(CxmlHeader header, CredentialMac mac, string sharedSecret)
    {
        var expected = Encoding.UTF8.GetBytes(Compute(header, mac, sharedSecret));
        var actual = Encoding.UTF8.GetBytes(mac.Value.Trim());
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    /// <summary>Lower-cases a domain unless it is known to be case-sensitive.</summary>
    public static string NormalizeDomain(string domain) =>
        CaseSensitiveDomains.TryGetValue(domain.Trim(), out var known) ? known : domain.Trim().ToLowerInvariant();

    /// <summary>Trims and lower-cases an identity.</summary>
    public static string NormalizeIdentity(string identity) => identity.Trim().ToLowerInvariant();
}
