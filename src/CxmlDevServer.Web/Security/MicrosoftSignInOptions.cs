namespace CxmlDevServer.Web.Security;

/// <summary>
/// Settings of the Microsoft 365 (Microsoft Entra ID) sign-in, bound from <c>Admin:Microsoft</c>. The sign-in is
/// single tenant: only accounts of <see cref="TenantId"/> can sign in. It is independent of the password login.
/// </summary>
public sealed class MicrosoftSignInOptions
{
    /// <summary>The Microsoft identity platform that is used unless <see cref="Instance"/> says otherwise.</summary>
    public const string DefaultInstance = "https://login.microsoftonline.com/";

    /// <summary>The tenant ids that are not a single tenant and are therefore rejected.</summary>
    private static readonly string[] MultiTenantAliases = ["common", "organizations", "consumers"];

    /// <summary>The directory (tenant) id of the Microsoft 365 organization, a GUID. Multi-tenant aliases are not accepted.</summary>
    public string? TenantId { get; set; }

    /// <summary>The application (client) id of the app registration, a GUID.</summary>
    public string? ClientId { get; set; }

    /// <summary>The client secret of the app registration. Keep it out of files: use <c>Admin__Microsoft__ClientSecret</c>.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>The base URL of the identity platform; only changes for sovereign clouds.</summary>
    public string Instance { get; set; } = DefaultInstance;

    /// <summary>The path of the sign-in callback (the redirect URI registered in Entra is the site URL plus this path).</summary>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>The name of the sign-in, for log messages and documentation.</summary>
    public string DisplayName { get; set; } = "Microsoft";

    /// <summary>
    /// True when any of tenant or client id is set: that is the intent to use the Microsoft sign-in, and
    /// <see cref="Validate"/> then demands the complete configuration instead of silently ignoring a half-set one.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) || !string.IsNullOrWhiteSpace(ClientId) || !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>The authority (issuer) of the tenant: <c>{Instance}/{TenantId}/v2.0</c>.</summary>
    public string Authority => $"{Instance.TrimEnd('/')}/{TenantId?.Trim()}/v2.0";

    /// <summary>Returns the reason why these settings are unusable, or null.</summary>
    public string? Validate()
    {
        if (!IsConfigured)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(TenantId))
        {
            return "Admin:Microsoft:TenantId must be set (the directory id of your Microsoft 365 tenant).";
        }

        var tenant = TenantId.Trim();
        if (MultiTenantAliases.Contains(tenant, StringComparer.OrdinalIgnoreCase))
        {
            return $"Admin:Microsoft:TenantId '{tenant}' is not supported: the sign-in is single tenant only. Use the tenant's GUID.";
        }

        if (!Guid.TryParse(tenant, out _))
        {
            return "Admin:Microsoft:TenantId must be the tenant's GUID (single tenant only).";
        }

        if (string.IsNullOrWhiteSpace(ClientId) || !Guid.TryParse(ClientId.Trim(), out _))
        {
            return "Admin:Microsoft:ClientId must be the application (client) id, a GUID.";
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            return "Admin:Microsoft:ClientSecret must be set (environment variable Admin__Microsoft__ClientSecret).";
        }

        if (!Uri.TryCreate(Instance, UriKind.Absolute, out var instance) || instance.Scheme != Uri.UriSchemeHttps)
        {
            return "Admin:Microsoft:Instance must be an absolute https URL.";
        }

        if (string.IsNullOrWhiteSpace(CallbackPath) || !CallbackPath.StartsWith('/'))
        {
            return "Admin:Microsoft:CallbackPath must start with '/'.";
        }

        return null;
    }
}
