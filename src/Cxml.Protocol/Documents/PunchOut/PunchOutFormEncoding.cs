namespace Cxml.Protocol.Documents.PunchOut;

/// <summary>How a <c>PunchOutOrderMessage</c> is carried in the browser form post.</summary>
public enum PunchOutFormEncoding
{
    /// <summary>Field <c>cXML-base64</c>: the document's UTF-8 bytes, base64 encoded (preferred).</summary>
    Base64,
    /// <summary>Field <c>cXML-urlencoded</c>: the document text as a us-ascii HTML attribute value.</summary>
    UrlEncoded,
}
