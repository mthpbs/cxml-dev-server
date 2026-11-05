using CxmlDevServer.Core.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Web.Services;

/// <summary>The URLs of this server as the browser (and the procurement system) see them.</summary>
public sealed class UiUrls
{
    private readonly NavigationManager _navigation;
    private readonly CxmlServerOptions _options;

    /// <summary>Creates the helper.</summary>
    public UiUrls(NavigationManager navigation, IOptions<CxmlServerOptions> options)
    {
        _navigation = navigation;
        _options = options.Value;
    }

    /// <summary>The base URL the browser used to reach the server (no trailing slash).</summary>
    public string RequestBaseUrl => _navigation.BaseUri.TrimEnd('/');

    /// <summary>The configured public base URL, or the request base URL.</summary>
    public string PublicBaseUrl => _options.ResolveBaseUrl(RequestBaseUrl);

    /// <summary>The cXML endpoint announced to clients.</summary>
    public string CxmlEndpoint => PublicBaseUrl + "/cxml";

    /// <summary>The cXML endpoint this server can call itself on (used by the sender).</summary>
    public string LocalCxmlEndpoint => RequestBaseUrl + "/cxml";
}
