using Cxml.Protocol.Documents.PunchOut;

namespace CxmlDevServer.Core.PunchOut;

/// <summary>
/// Everything needed to render the auto-submitting browser form that returns a PunchOutOrderMessage to the buyer.
/// </summary>
/// <param name="FormActionUrl">The buyer's BrowserFormPost URL (the form's <c>action</c>).</param>
/// <param name="FieldName">The hidden field name: <c>cXML-base64</c> or <c>cXML-urlencoded</c>.</param>
/// <param name="FieldValue">
/// The value to write into the hidden field's <c>value</c> attribute verbatim. For <c>cXML-urlencoded</c> it is already
/// HTML-encoded (us-ascii with entities) as the specification requires, so it must not be encoded a second time.
/// </param>
/// <param name="Encoding">The encoding in use.</param>
/// <param name="Xml">The PunchOutOrderMessage document (for a debug view).</param>
/// <param name="LogId">The id of the outbound message log entry.</param>
/// <param name="Cancelled">True when the user cancelled instead of checking out.</param>
public sealed record PunchOutReturn(
    string FormActionUrl,
    string FieldName,
    string FieldValue,
    PunchOutFormEncoding Encoding,
    string Xml,
    int LogId,
    bool Cancelled);
