using System.Xml.Linq;

namespace Cxml.Protocol.Internal;

/// <summary>Commonly used qualified XML names.</summary>
internal static class XmlNames
{
    /// <summary>The <c>xml:lang</c> attribute.</summary>
    public static readonly XName Lang = XNamespace.Xml + "lang";
}
