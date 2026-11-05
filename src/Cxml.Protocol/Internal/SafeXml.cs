using System.Xml;
using System.Xml.Linq;

namespace Cxml.Protocol.Internal;

/// <summary>XML loading with DTD processing disabled and no external resolution.</summary>
internal static class SafeXml
{
    /// <summary>Reader settings that never parse a DTD or touch external resources.</summary>
    public static XmlReaderSettings ReaderSettings() => new()
    {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        MaxCharactersFromEntities = 0,
        CloseInput = false,
    };

    /// <summary>Parses an XML string into a document, keeping line information.</summary>
    public static XDocument ParseDocument(TextReader reader, LoadOptions options = LoadOptions.SetLineInfo)
    {
        using var xml = XmlReader.Create(reader, ReaderSettings());
        return XDocument.Load(xml, options);
    }

    /// <summary>Parses an XML string into an element.</summary>
    public static XElement ParseElement(string text, LoadOptions options = LoadOptions.None)
    {
        using var xml = XmlReader.Create(new StringReader(text), ReaderSettings());
        return XElement.Load(xml, options);
    }
}
