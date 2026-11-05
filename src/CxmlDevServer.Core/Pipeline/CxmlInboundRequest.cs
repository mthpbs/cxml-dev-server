using CxmlDevServer.Core.Domain;

namespace CxmlDevServer.Core.Pipeline;

/// <summary>What the HTTP layer hands to the <see cref="CxmlInboundProcessor"/>.</summary>
public sealed class CxmlInboundRequest
{
    /// <summary>The request body as received.</summary>
    public byte[] Body { get; init; } = [];

    /// <summary>The request <c>Content-Type</c> header.</summary>
    public string? ContentType { get; init; }

    /// <summary>The caller's IP endpoint, for the message log.</summary>
    public string? RemoteEndpoint { get; init; }

    /// <summary>
    /// The scheme, host and path base of this server as seen by the caller (e.g. <c>https://localhost:5001</c>),
    /// used to build StartPage and Profile URLs when no <c>PublicBaseUrl</c> is configured.
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Parsed form fields when the body was a form post; a <c>cXML-base64</c> or <c>cXML-urlencoded</c>
    /// field is then used as the document. When <c>null</c> and the content type is form-encoded the body is parsed.
    /// </summary>
    public IReadOnlyCollection<KeyValuePair<string, string?>>? FormFields { get; init; }

    /// <summary>The transport recorded in the message log.</summary>
    public MessageChannel Channel { get; init; } = MessageChannel.Http;
}
