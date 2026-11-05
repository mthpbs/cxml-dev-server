namespace Cxml.Protocol.Writing;

/// <summary>Options controlling <see cref="CxmlWriter"/> output.</summary>
public sealed class CxmlWriterOptions
{
    /// <summary>
    /// Indent the output for readability. Off by default so ANY-content such as <c>BuyerCookie</c>
    /// is written byte-for-byte as stored.
    /// </summary>
    public bool Indent { get; set; }

    /// <summary>
    /// The DTD URL placed in the DOCTYPE. Defaults to the cXML 1.2.071 DTD
    /// (<c>http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd</c>).
    /// </summary>
    public string DtdUrl { get; set; } = CxmlVersion.DtdUrl();
}
