namespace CxmlDevServer.Core.Domain;

/// <summary>How a trading partner proves its identity on inbound documents.</summary>
public enum AuthMode
{
    /// <summary>The Sender credential must carry the partner's <c>SharedSecret</c>.</summary>
    SharedSecret,

    /// <summary>The Sender credential identifies the partner but no secret is checked.</summary>
    None,
}

/// <summary>How strictly inbound documents from a partner are validated.</summary>
public enum ValidationMode
{
    /// <summary>Any DTD or business-rule error is answered with <c>406</c>; a wrong <c>To</c> identity with <c>401</c>.</summary>
    Strict,

    /// <summary>Documents are accepted; problems are recorded as validation issues only.</summary>
    Lenient,

    /// <summary>No DTD or rule validation is recorded or enforced.</summary>
    Off,
}

/// <summary>The form field used when a PunchOutOrderMessage is posted back to the buyer.</summary>
public enum PunchOutPostEncoding
{
    /// <summary><c>cXML-base64</c>: the document is base64 encoded (preferred by the spec).</summary>
    Base64,

    /// <summary><c>cXML-urlencoded</c>: the document is HTML-encoded, us-ascii, with entities.</summary>
    UrlEncoded,
}

/// <summary>Lifecycle of a PunchOut shopping session.</summary>
public enum PunchOutSessionStatus
{
    /// <summary>Set up by a PunchOutSetupRequest; the StartPage has not been opened yet.</summary>
    Created,

    /// <summary>The StartPage has been opened by the user.</summary>
    Opened,

    /// <summary>The cart has been returned to the buyer.</summary>
    Submitted,

    /// <summary>The user left without ordering.</summary>
    Cancelled,

    /// <summary>The session passed its expiry time.</summary>
    Expired,
}

/// <summary>Lifecycle of a received purchase order.</summary>
public enum PurchaseOrderStatus
{
    /// <summary>A new order was received.</summary>
    Received,

    /// <summary>A later version of the order was received.</summary>
    Updated,

    /// <summary>The buyer cancelled the order (<c>type="delete"</c>).</summary>
    Cancelled,

    /// <summary>The order was rejected.</summary>
    Rejected,
}

/// <summary>Whether a logged message was received or sent by this server.</summary>
public enum MessageDirection
{
    /// <summary>Received from a buyer.</summary>
    Inbound,

    /// <summary>Sent by this server.</summary>
    Outbound,
}

/// <summary>The transport a logged message travelled over.</summary>
public enum MessageChannel
{
    /// <summary>A direct HTTP POST of a cXML document.</summary>
    Http,

    /// <summary>A PunchOutOrderMessage form-posted through the user's browser.</summary>
    BrowserPost,

    /// <summary>A document sent by hand from the message sender tool.</summary>
    ManualSend,
}

/// <summary>What a response scenario does to a matching request.</summary>
public enum ScenarioBehavior
{
    /// <summary>Answer with the configured status instead of processing the request.</summary>
    Respond,

    /// <summary>Apply the delay only, then process the request normally.</summary>
    Passthrough,

    /// <summary>Answer with a deliberately malformed (non-XML or truncated) body.</summary>
    MalformedBody,

    /// <summary>Never answer: after the delay the connection is aborted (simulates a timeout).</summary>
    NoResponse,
}
