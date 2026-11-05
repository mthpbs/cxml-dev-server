using System.Text;
using System.Text.RegularExpressions;

namespace CxmlDevServer.Core.Common;

/// <summary>Turns raw request bytes into text, honouring a byte order mark or the encoding in the XML declaration.</summary>
public static partial class XmlTextDecoder
{
    /// <summary>Decodes <paramref name="bytes"/>; falls back to UTF-8 for unknown encodings.</summary>
    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes[3..]);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes[2..]);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);
        }

        var head = Encoding.ASCII.GetString(bytes[..Math.Min(bytes.Length, 200)]);
        var match = DeclaredEncodingRegex().Match(head);
        if (match.Success)
        {
            try
            {
                return Encoding.GetEncoding(match.Groups[1].Value).GetString(bytes);
            }
            catch (ArgumentException)
            {
                // Unknown encoding name: fall through to UTF-8.
            }
        }

        return Encoding.UTF8.GetString(bytes);
    }

    [GeneratedRegex("""^\s*<\?xml[^>]*encoding\s*=\s*["']([A-Za-z0-9._\-]+)["']""", RegexOptions.CultureInvariant)]
    private static partial Regex DeclaredEncodingRegex();
}
