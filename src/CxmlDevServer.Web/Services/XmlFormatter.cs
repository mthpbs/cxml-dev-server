using System.Xml;
using System.Xml.Linq;

namespace CxmlDevServer.Web.Services;

/// <summary>Formats XML for display.</summary>
public static class XmlFormatter
{
    /// <summary>
    /// Returns <paramref name="xml"/> indented. Input that is not well-formed XML is returned unchanged, so the
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
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            var document = XDocument.Load(reader);
            var declaration = document.Declaration is null ? string.Empty : document.Declaration + Environment.NewLine;
            var doctype = document.DocumentType is { } dt
                ? $"<!DOCTYPE {dt.Name} SYSTEM \"{dt.SystemId}\">{Environment.NewLine}"
                : string.Empty;
            return declaration + doctype + document.Root?.ToString(SaveOptions.None);
        }
        catch (XmlException)
        {
            return xml;
        }
    }
}
