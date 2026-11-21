using CxmlDevServer.Core.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CxmlDevServer.Web.Security;

/// <summary>Checks a submitted user name and password against the <see cref="AdminOptions"/>.</summary>
public sealed class AdminCredentialValidator
{
    private readonly IOptions<AdminOptions> _options;
    private readonly PasswordHasher<string> _hasher = new();

    /// <summary>Creates the validator.</summary>
    public AdminCredentialValidator(IOptions<AdminOptions> options) => _options = options;

    /// <summary>
    /// True when the credentials are right. The user name and the password are always both compared, in constant
    /// time, so the response does not reveal which of the two was wrong.
    /// </summary>
    public bool IsValid(string? username, string? password)
    {
        var admin = _options.Value;
        // Without a configured password (Microsoft-only sign-in) the password login must stay closed: an empty
        // submitted password must never match an empty configured one.
        if (!admin.Enabled || !admin.HasPassword)
        {
            return false;
        }

        var userOk = SecretComparer.FixedTimeEquals(admin.Username, username);
        var passwordOk = VerifyPassword(admin, password ?? string.Empty);
        return userOk & passwordOk;
    }

    private bool VerifyPassword(AdminOptions admin, string password)
    {
        if (!string.IsNullOrEmpty(admin.PasswordHash))
        {
            try
            {
                return _hasher.VerifyHashedPassword(admin.Username, admin.PasswordHash, password) != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Plain-text fallback for secret stores; compared via fixed-length digests.
        return SecretComparer.FixedTimeEquals(admin.Password, password);
    }

    /// <summary>Creates the hash that goes into <c>Admin:PasswordHash</c>.</summary>
    public static string Hash(string password) => new PasswordHasher<string>().HashPassword(string.Empty, password);
}
