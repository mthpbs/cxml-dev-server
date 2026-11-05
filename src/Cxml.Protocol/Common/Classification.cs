using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>A commodity classification, e.g. <c>&lt;Classification domain="UNSPSC"&gt;43211503&lt;/Classification&gt;</c>.</summary>
public sealed class Classification
{
    /// <summary>Creates an empty classification.</summary>
    public Classification() { }

    /// <summary>Creates a classification in <paramref name="domain"/> with the given element value.</summary>
    public Classification(string domain, string value)
    {
        Domain = domain;
        Value = value;
    }

    /// <summary>The classification scheme, e.g. <c>UNSPSC</c>.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Optional <c>code</c> attribute.</summary>
    public string? Code { get; set; }

    /// <summary>The element text (usually the code itself).</summary>
    public string Value { get; set; } = string.Empty;

    internal static Classification Parse(XElement e) => new()
    {
        Domain = e.Attr("domain") ?? string.Empty,
        Code = e.Attr("code"),
        Value = e.Value.Trim(),
    };

    internal XElement ToXml()
    {
        var e = new XElement("Classification", new XAttribute("domain", Domain));
        e.AddAttr("code", Code);
        e.Add(Value);
        return e;
    }
}
