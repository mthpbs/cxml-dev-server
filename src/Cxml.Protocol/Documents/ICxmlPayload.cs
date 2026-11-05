namespace Cxml.Protocol.Documents;

/// <summary>
/// A cXML document payload: the single element inside <c>Request</c>, <c>Response</c> or <c>Message</c>
/// (for example <c>OrderRequest</c> or <c>PunchOutSetupResponse</c>).
/// </summary>
public interface ICxmlPayload
{
    /// <summary>The XML element name of the payload, e.g. <c>OrderRequest</c>.</summary>
    string ElementName { get; }
}
