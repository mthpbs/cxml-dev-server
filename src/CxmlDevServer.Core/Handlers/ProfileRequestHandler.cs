using Cxml.Protocol.Documents.Profile;
using CxmlDevServer.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace CxmlDevServer.Core.Handlers;

/// <summary>
/// Handles <c>ProfileRequest</c> (which every cXML server must support): lists the transactions this server accepts
/// and the URL to send them to. Because it enumerates the registered handlers it is also the reference example of a
/// transaction registered with <c>AddCxmlTransaction&lt;THandler&gt;()</c>.
/// </summary>
public sealed class ProfileRequestHandler : ICxmlRequestHandler
{
    private readonly IServiceProvider _services;
    private readonly TimeProvider _time;

    /// <summary>Creates the handler.</summary>
    public ProfileRequestHandler(IServiceProvider services, TimeProvider time)
    {
        _services = services;
        _time = time;
    }

    /// <inheritdoc />
    public string RequestName => ProfileRequest.Name;

    /// <inheritdoc />
    public Task<CxmlHandlerResult> HandleAsync(CxmlInboundContext context)
    {
        // Resolved lazily: this handler is itself one of the registered handlers.
        var url = $"{context.BaseUrl}/cxml";
        var transactions = _services.GetServices<ICxmlRequestHandler>()
            .Select(h => h.RequestName)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(name => new ProfileTransaction { RequestName = name, Url = url })
            .ToList();

        var profile = new ProfileResponse
        {
            EffectiveDate = _time.GetLocalNow(),
            Transactions = transactions,
        };
        return Task.FromResult(CxmlHandlerResult.Ok(profile));
    }
}
