using System.Xml.Linq;

namespace Cxml.Protocol.Documents;

/// <summary>Turns a payload element into a model. Registered in a <see cref="CxmlPayloadRegistry"/>.</summary>
public interface ICxmlPayloadParser
{
    /// <summary>The payload element name this parser handles.</summary>
    string ElementName { get; }

    /// <summary>Parses <paramref name="element"/> (whose name equals <see cref="ElementName"/>).</summary>
    ICxmlPayload Parse(XElement element);
}
