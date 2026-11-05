using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Envelope;

/// <summary>The cXML <c>Status</c> element: code, text and optional detail in the element body.</summary>
public sealed class Status
{
    /// <summary>Numeric status code, see <see cref="CxmlStatusCodes"/>.</summary>
    public int Code { get; set; }

    /// <summary>Short text for the code.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Additional human-readable detail (element body); <c>null</c> when empty.</summary>
    public string? Detail { get; set; }

    /// <summary>The <c>xml:lang</c> of the text.</summary>
    public string? Lang { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    /// <summary>True for 2xx codes.</summary>
    public bool IsSuccess => CxmlStatusCodes.IsSuccess(Code);

    internal static Status Parse(XElement e)
    {
        var detail = e.Value.Trim();
        return new Status
        {
            Code = e.Attr("code").ToIntOrNull() ?? 0,
            Text = e.Attr("text") ?? string.Empty,
            Detail = detail.Length == 0 ? null : detail,
            Lang = e.Attribute(XmlNames.Lang)?.Value,
            Raw = e,
        };
    }

    internal XElement ToXml()
    {
        var e = new XElement("Status",
            new XAttribute("code", Code),
            new XAttribute("text", Text));
        if (!string.IsNullOrEmpty(Lang))
        {
            e.Add(new XAttribute(XmlNames.Lang, Lang));
        }

        if (!string.IsNullOrEmpty(Detail))
        {
            e.Add(Detail);
        }

        return e;
    }
}
