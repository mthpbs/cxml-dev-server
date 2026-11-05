using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A free-form name/value pair (<c>&lt;Extrinsic name="..."&gt;value&lt;/Extrinsic&gt;</c>).</summary>
public sealed class Extrinsic
{
    /// <summary>Creates an empty extrinsic.</summary>
    public Extrinsic() { }

    /// <summary>Creates an extrinsic with a text value.</summary>
    public Extrinsic(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>The <c>name</c> attribute.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The text value (whitespace-trimmed when parsed).</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The source element when parsed from XML. Extrinsic content is <c>ANY</c>; when the source
    /// contains child elements the writer re-emits that content instead of <see cref="Value"/>.
    /// </summary>
    public XElement? Raw { get; set; }

    internal static Extrinsic Parse(XElement e) => new()
    {
        Name = e.Attr("name") ?? string.Empty,
        Value = e.Value.Trim(),
        Raw = e,
    };

    internal static List<Extrinsic> ParseAll(XElement parent) =>
        parent.Elements("Extrinsic").Select(Parse).ToList();

    internal XElement ToXml()
    {
        var e = new XElement("Extrinsic", new XAttribute("name", Name));
        if (Raw is not null && Raw.HasElements)
        {
            e.Add(Raw.Nodes());
        }
        else
        {
            e.Add(Value);
        }

        return e;
    }
}
