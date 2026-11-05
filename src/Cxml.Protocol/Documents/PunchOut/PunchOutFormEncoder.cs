using System.Net;
using System.Text;

namespace Cxml.Protocol.Documents.PunchOut;

/// <summary>
/// Encodes and decodes the hidden form field that carries a <c>PunchOutOrderMessage</c> back to the
/// buyer's <c>BrowserFormPost</c> URL. Independent of ASP.NET.
/// </summary>
public static class PunchOutFormEncoder
{
    /// <summary>Name of the base64 form field.</summary>
    public const string Base64FieldName = "cXML-base64";

    /// <summary>Name of the url-encoded form field.</summary>
    public const string UrlEncodedFieldName = "cXML-urlencoded";

    /// <summary>Encodes a document as base64 of its UTF-8 bytes (value of <c>cXML-base64</c>).</summary>
    public static string EncodeBase64(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
    }

    /// <summary>
    /// Produces the text to place in the <c>value="..."</c> attribute of the <c>cXML-urlencoded</c> hidden
    /// input: HTML-attribute-encoded, with every non-ASCII character written as a numeric character
    /// reference (<c>&amp;#NNN;</c>) so the value is pure us-ascii. The browser URL-encodes it when
    /// submitting the form; the server must not URL-encode it again.
    /// </summary>
    public static string EncodeUrlEncodedFieldValue(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var builder = new StringBuilder(xml.Length + 64);
        for (var i = 0; i < xml.Length; i++)
        {
            var c = xml[i];
            switch (c)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '"': builder.Append("&quot;"); break;
                case '\'': builder.Append("&#39;"); break;
                default:
                    if (c < 0x80)
                    {
                        builder.Append(c);
                    }
                    else if (char.IsHighSurrogate(c) && i + 1 < xml.Length && char.IsLowSurrogate(xml[i + 1]))
                    {
                        builder.Append("&#").Append(char.ConvertToUtf32(c, xml[i + 1])).Append(';');
                        i++;
                    }
                    else
                    {
                        builder.Append("&#").Append((int)c).Append(';');
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>Returns the form field name and (HTML-attribute-ready) value for <paramref name="xml"/>.</summary>
    public static KeyValuePair<string, string> Encode(string xml, PunchOutFormEncoding encoding) => encoding switch
    {
        PunchOutFormEncoding.Base64 => new(Base64FieldName, EncodeBase64(xml)),
        PunchOutFormEncoding.UrlEncoded => new(UrlEncodedFieldName, EncodeUrlEncodedFieldValue(xml)),
        _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null),
    };

    /// <summary>
    /// Decodes the cXML document from posted form fields. <c>cXML-base64</c> is checked first, then
    /// <c>cXML-urlencoded</c>; field names are case-insensitive. Returns <c>null</c> when neither is present.
    /// </summary>
    /// <param name="fields">The posted fields, with URL decoding (done by the web framework) already applied.</param>
    /// <exception cref="FormatException">The base64 field does not contain valid base64.</exception>
    public static string? Decode(IEnumerable<KeyValuePair<string, string?>> fields) =>
        TryDecode(fields, out var xml, out _) ? xml : null;

    /// <summary>Like <see cref="Decode"/>, also reporting which field carried the document.</summary>
    public static bool TryDecode(
        IEnumerable<KeyValuePair<string, string?>> fields,
        out string xml,
        out PunchOutFormEncoding encoding)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var list = fields as IReadOnlyCollection<KeyValuePair<string, string?>> ?? fields.ToList();

        var base64 = Find(list, Base64FieldName);
        if (base64 is not null)
        {
            // A sloppy URL decoder turns '+' into ' '; undo that and ignore line breaks.
            var cleaned = new string(base64.Where(c => !char.IsWhiteSpace(c) || c == ' ').ToArray()).Replace(' ', '+');
            xml = Encoding.UTF8.GetString(Convert.FromBase64String(cleaned)).TrimStart('﻿');
            encoding = PunchOutFormEncoding.Base64;
            return true;
        }

        var urlEncoded = Find(list, UrlEncodedFieldName);
        if (urlEncoded is not null)
        {
            xml = urlEncoded.TrimStart('﻿');
            encoding = PunchOutFormEncoding.UrlEncoded;
            return true;
        }

        xml = string.Empty;
        encoding = default;
        return false;
    }

    /// <summary>
    /// Decodes a <c>cXML-urlencoded</c> value that is still HTML-encoded (as it appears inside the
    /// <c>value</c> attribute of the generated form) back to the XML text.
    /// </summary>
    public static string DecodeUrlEncodedFieldValue(string attributeValue)
    {
        ArgumentNullException.ThrowIfNull(attributeValue);
        return WebUtility.HtmlDecode(attributeValue);
    }

    private static string? Find(IEnumerable<KeyValuePair<string, string?>> fields, string name) =>
        fields.FirstOrDefault(f => string.Equals(f.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
}
