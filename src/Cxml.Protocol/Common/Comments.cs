using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>Free-text <c>Comments</c> (attachments are not modelled but remain visible in <see cref="Raw"/>).</summary>
public sealed class Comments
{
    /// <summary>The comment text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The <c>xml:lang</c> attribute.</summary>
    public string? Lang { get; set; }

    /// <summary>The optional <c>type</c> attribute.</summary>
    public string? Type { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static Comments Parse(XElement e) => new()
    {
        Text = string.Concat(e.Nodes().OfType<XText>().Select(t => t.Value)).Trim(),
        Lang = e.LangAttr(),
        Type = e.Attr("type"),
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("Comments");
        e.AddAttr(XmlNames.Lang, Lang);
        e.AddAttr("type", Type);
        e.Add(Text);
        return e;
    }
}
