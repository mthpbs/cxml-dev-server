using System.Xml.Linq;

namespace Cxml.Protocol.Documents;

/// <summary>
/// A payload element without a registered parser. The reader never throws for these so a server
/// can answer "450 Not Implemented"; the original XML is kept in <see cref="Raw"/>.
/// </summary>
public sealed class UnknownPayload : ICxmlPayload
{
    /// <summary>Creates an unknown payload for <paramref name="raw"/>.</summary>
    public UnknownPayload(XElement raw) => Raw = raw;

    /// <inheritdoc />
    public string ElementName => Raw.Name.LocalName;

    /// <summary>The unparsed payload element.</summary>
    public XElement Raw { get; }
}
