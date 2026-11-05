using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Cxml.Protocol.Internal;

/// <summary>Small helpers shared by the model parsers and writers.</summary>
internal static class XmlExtensions
{
    /// <summary>Returns an attribute value or <c>null</c>.</summary>
    public static string? Attr(this XElement e, string name) => e.Attribute(name)?.Value;

    /// <summary>Returns the trimmed text of a child element, or <c>null</c> if it is absent.</summary>
    public static string? ChildText(this XElement e, string name) => e.Element(name)?.Value.Trim();

    /// <summary>Returns the <c>xml:lang</c> attribute value or <c>null</c>.</summary>
    public static string? LangAttr(this XElement e) => e.Attribute(XmlNames.Lang)?.Value;

    /// <summary>Parses an integer using the invariant culture.</summary>
    public static int? ToIntOrNull(this string? s) =>
        int.TryParse(s?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>Parses a decimal using the invariant culture.</summary>
    public static decimal? ToDecimalOrNull(this string? s) =>
        decimal.TryParse(s?.Trim(), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>Formats a decimal with the invariant culture (preserving its scale).</summary>
    public static string ToInvariantString(this decimal value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Formats an integer with the invariant culture.</summary>
    public static string ToInvariantString(this int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Adds an attribute only when the value is not null or empty.</summary>
    public static void AddAttr(this XElement e, XName name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            e.Add(new XAttribute(name, value));
        }
    }

    /// <summary>Adds a child element only when it is not null.</summary>
    public static void AddIfNotNull(this XElement e, XElement? child)
    {
        if (child is not null)
        {
            e.Add(child);
        }
    }

    /// <summary>The markup between the start and end tag of an element (round-trips ANY content).</summary>
    public static string InnerXml(this XElement e) =>
        string.Concat(e.Nodes().Select(n => n.ToString(SaveOptions.DisableFormatting)));

    /// <summary>
    /// Turns stored inner markup back into nodes. Falls back to plain text when the string is not
    /// a well-formed XML fragment.
    /// </summary>
    public static IEnumerable<XNode> ParseInnerXml(string? markup)
    {
        if (string.IsNullOrEmpty(markup))
        {
            return [];
        }

        try
        {
            var wrapper = SafeXml.ParseElement("<w>" + markup + "</w>", LoadOptions.PreserveWhitespace);
            return wrapper.Nodes().ToList();
        }
        catch (XmlException)
        {
            return [new XText(markup)];
        }
    }
}
