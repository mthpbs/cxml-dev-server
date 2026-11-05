using System.Xml.Linq;

namespace Cxml.Protocol.Documents;

/// <summary>Turns a payload model into XML. Registered in a <see cref="CxmlPayloadRegistry"/>.</summary>
public interface ICxmlPayloadWriter
{
    /// <summary>The payload element name this writer produces.</summary>
    string ElementName { get; }

    /// <summary>
    /// Builds the payload element. Child element order must follow the cXML DTD content model.
    /// </summary>
    XElement Write(ICxmlPayload payload);
}
