using CxmlDevServer.Core.Pipeline;

namespace CxmlDevServer.Core.Handlers;

/// <summary>
/// Handles one kind of inbound request, identified by the payload element name inside <c>Request</c>.
/// Register with <c>AddCxmlTransaction&lt;THandler&gt;()</c>.
/// </summary>
public interface ICxmlRequestHandler
{
    /// <summary>The payload element name handled, e.g. <c>OrderRequest</c>.</summary>
    string RequestName { get; }

    /// <summary>Handles the request found in <see cref="CxmlInboundContext.Envelope"/>.</summary>
    Task<CxmlHandlerResult> HandleAsync(CxmlInboundContext context);
}
