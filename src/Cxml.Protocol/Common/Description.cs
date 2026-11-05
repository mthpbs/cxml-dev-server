using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>An item description with optional <c>ShortName</c>.</summary>
public sealed class Description
{
    /// <summary>Creates an empty description.</summary>
    public Description() { }

    /// <summary>Creates a description.</summary>
    public Description(string lang, string text, string? shortName = null)
    {
        Lang = lang;
        Text = text;
        ShortName = shortName;
    }

    /// <summary>The <c>xml:lang</c> (required by the DTD).</summary>
    public string Lang { get; set; } = "en";

    /// <summary>The description text (whitespace-trimmed when parsed).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The optional <c>ShortName</c> child.</summary>
    public string? ShortName { get; set; }

    /// <summary>The optional <c>type</c> qualifier.</summary>
    public string? Type { get; set; }

    internal static Description Parse(XElement e) => new()
    {
        Lang = e.LangAttr() ?? "en",
        Text = string.Concat(e.Nodes().OfType<XText>().Select(t => t.Value)).Trim(),
        ShortName = e.ChildText("ShortName"),
        Type = e.Attr("type"),
    };

    internal XElement ToXml(string elementName = "Description")
    {
        var e = new XElement(elementName, new XAttribute(XmlNames.Lang, Lang));
        e.AddAttr("type", Type);
        e.Add(Text);
        if (!string.IsNullOrEmpty(ShortName))
        {
            e.Add(new XElement("ShortName", ShortName));
        }

        return e;
    }
}
