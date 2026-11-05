using CxmlDevServer.Core.Domain;

namespace CxmlDevServer.Core.Templates;

/// <summary>Describes a message template offered by the manual sender.</summary>
/// <param name="Key">Identifier passed to <see cref="IMessageTemplateService.RenderAsync"/>.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">What the template is for.</param>
/// <param name="ExpectsPunchOutResponse">True when the reply contains a StartPage URL the user may want to open.</param>
public sealed record MessageTemplate(string Key, string Name, string Description, bool ExpectsPunchOutResponse = false);

/// <summary>Produces ready-to-send cXML documents for the manual sender.</summary>
public interface IMessageTemplateService
{
    /// <summary>All available templates.</summary>
    IReadOnlyList<MessageTemplate> Templates { get; }

    /// <summary>
    /// Renders a template: fills payloadID, timestamp, the partner's credentials and secret (demo values when
    /// <paramref name="partner"/> is <c>null</c>), a unique orderID or BuyerCookie, and a BrowserFormPost URL that
    /// points at this server's built-in test buyer receiver.
    /// </summary>
    /// <param name="templateKey">One of <see cref="TemplateKeys"/>.</param>
    /// <param name="partner">The sending partner.</param>
    /// <param name="targetBaseUrl">The base URL of this server (used for BrowserFormPost unless <c>PublicBaseUrl</c> is configured).</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <exception cref="ArgumentException">The template key is unknown.</exception>
    Task<string> RenderAsync(string templateKey, TradingPartner? partner, string targetBaseUrl, CancellationToken cancellationToken = default);
}

/// <summary>The keys of the built-in templates.</summary>
public static class TemplateKeys
{
    /// <summary>PunchOutSetupRequest, operation create.</summary>
    public const string PunchOutSetupCreate = "punchout-setup-create";

    /// <summary>PunchOutSetupRequest, operation edit.</summary>
    public const string PunchOutSetupEdit = "punchout-setup-edit";

    /// <summary>PunchOutSetupRequest, operation inspect.</summary>
    public const string PunchOutSetupInspect = "punchout-setup-inspect";

    /// <summary>OrderRequest.</summary>
    public const string OrderRequest = "order-request";

    /// <summary>ProfileRequest.</summary>
    public const string ProfileRequest = "profile-request";
}
