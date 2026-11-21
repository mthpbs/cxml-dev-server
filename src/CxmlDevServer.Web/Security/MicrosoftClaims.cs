using System.Security.Claims;

namespace CxmlDevServer.Web.Security;

/// <summary>
/// Turns the claims of a validated Microsoft ID token into the slim principal that goes into the login cookie, and
/// refuses tokens of any other tenant.
/// </summary>
internal static class MicrosoftClaims
{
    /// <summary>The authentication type of the identity created for a Microsoft sign-in.</summary>
    public const string AuthenticationType = "Microsoft";

    /// <summary>The ID token claim with the tenant (directory) id.</summary>
    public const string TenantClaim = "tid";

    /// <summary>The ID token claim with the object id of the user in the directory (stable per user and tenant).</summary>
    public const string ObjectIdClaim = "oid";

    /// <summary>The claim that carries the display name of the user.</summary>
    public const string DisplayNameClaim = "name";

    /// <summary>
    /// Returns the principal for the cookie, or null when the token does not come from <paramref name="tenantId"/>
    /// or has no object id. The signature and the issuer are already checked by the OpenID Connect handler; the
    /// <c>tid</c> comparison is a second, explicit guard that only accounts of the configured tenant get in.
    /// </summary>
    public static ClaimsPrincipal? CreatePrincipal(ClaimsPrincipal idToken, string tenantId)
    {
        var tid = idToken.FindFirst(TenantClaim)?.Value;
        var oid = idToken.FindFirst(ObjectIdClaim)?.Value;
        if (!SameGuid(tid, tenantId) || string.IsNullOrWhiteSpace(oid))
        {
            return null;
        }

        var displayName = idToken.FindFirst(DisplayNameClaim)?.Value;
        var name = FirstNonEmpty(idToken.FindFirst("preferred_username")?.Value, idToken.FindFirst("email")?.Value, displayName, oid)!;

        // Only what the application needs: it keeps the cookie small and leaves no tokens behind.
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, name),
            new(ObjectIdClaim, oid),
            new(TenantClaim, tid!),
            new(AdminAuthExtensions.AuthMethodClaim, AdminAuthExtensions.AuthMethodMicrosoft),
        };
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            claims.Add(new Claim(DisplayNameClaim, displayName));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType, ClaimTypes.Name, ClaimTypes.Role));
    }

    private static bool SameGuid(string? a, string? b) =>
        Guid.TryParse(a, out var left) && Guid.TryParse(b, out var right) && left == right;

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
