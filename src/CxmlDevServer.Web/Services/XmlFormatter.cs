using System.Xml;
using System.Xml.Linq;

namespace CxmlDevServer.Web.Services;

/// <summary>Formats XML for display.</summary>
public static class XmlFormatter
{
    /// <summary>
    /// Returns <paramref name="xml"/> indented. Everything before the root element (XML declaration, DOCTYPE,
    /// comments) is kept exactly as written. Input that is not well-formed XML is returned unchanged, so the
    /// text of a broken document can always be inspected.
    /// </summary>
    public static string Pretty(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return xml ?? string.Empty;
        }

        try
        {
            // DtdProcessing.Ignore never loads the DTD, but still reports the DOCTYPE node (and the document
            // is not validated), so well-formed cXML is formatted without any I/O.
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            var rootStart = -1;
            XElement? root = null;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    var info = (IXmlLineInfo)reader;
                    rootStart = OffsetOf(xml, info.LineNumber, info.LinePosition) - 2; // LinePosition points at the element name, one past the "<"
                    root = (XElement)XNode.ReadFrom(reader);
                    break;
                }
            }

            if (root is null || rootStart < 0)
            {
                return xml;
            }

            var prolog = xml[..rootStart].TrimEnd();
            var body = root.ToString(SaveOptions.None);
            return prolog.Length == 0 ? body : prolog + Environment.NewLine + body;
        }
        catch (XmlException)
        {
            return xml;
        }
    }

    /// <summary>Converts a 1-based line and column (as reported by <see cref="IXmlLineInfo"/>) to a string index.</summary>
    private static int OffsetOf(string text, int line, int column)
    {
        var index = 0;
        for (var current = 1; current < line && index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                current++;
            }
            else if (text[index] == '\r')
            {
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                current++;
            }
        }

        return index + column;
    }
}
