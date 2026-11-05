using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Envelope;

/// <summary>The <c>CredentialMac</c> authentication element (HMAC-SHA1-96 message authentication code).</summary>
public sealed class CredentialMac
{
    /// <summary>MAC type; only <c>FromSenderCredentials</c> is defined.</summary>
    public string Type { get; set; } = "FromSenderCredentials";

    /// <summary>MAC algorithm; only <c>HMAC-SHA1-96</c> is defined.</summary>
    public string Algorithm { get; set; } = "HMAC-SHA1-96";

    /// <summary>Creation date exactly as it appears in the document.</summary>
    public string CreationDate { get; set; } = string.Empty;

    /// <summary>Expiration date exactly as it appears in the document.</summary>
    public string ExpirationDate { get; set; } = string.Empty;

    /// <summary>The base64 MAC value.</summary>
    public string Value { get; set; } = string.Empty;

    internal static CredentialMac Parse(XElement e) => new()
    {
        Type = e.Attr("type") ?? string.Empty,
        Algorithm = e.Attr("algorithm") ?? string.Empty,
        CreationDate = e.Attr("creationDate") ?? string.Empty,
        ExpirationDate = e.Attr("expirationDate") ?? string.Empty,
        Value = e.Value.Trim(),
    };

    internal XElement ToXml() => new("CredentialMac",
        new XAttribute("type", Type),
        new XAttribute("algorithm", Algorithm),
        new XAttribute("creationDate", CreationDate),
        new XAttribute("expirationDate", ExpirationDate),
        Value);
}
