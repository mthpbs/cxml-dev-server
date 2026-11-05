using Microsoft.AspNetCore.Identity;

namespace CxmlDevServer.Web.Security;

/// <summary>
/// Settings of the optional admin login, bound from the <c>Admin</c> configuration section. When
/// <see cref="Enabled"/> is false (the default) the admin UI is open, as it is for local runs.
/// </summary>
public sealed class AdminOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Admin";

    /// <summary>True: the admin UI requires a login. The PunchOut shop, the cXML endpoints and the test buyer stay anonymous.</summary>
    public bool Enabled { get; set; }

    /// <summary>The user name of the single admin account.</summary>
    public string Username { get; set; } = "admin";

    /// <summary>
    /// The password as an ASP.NET Core Identity <see cref="PasswordHasher{TUser}"/> (format v3) hash, as printed by
    /// <c>hash-password</c>. Preferred over <see cref="Password"/>.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>The password in clear text; a fallback for secret stores. Ignored when <see cref="PasswordHash"/> is set.</summary>
    public string? Password { get; set; }

    /// <summary>The sliding lifetime of the login cookie, in hours.</summary>
    public int SessionHours { get; set; } = 8;

    /// <summary>
    /// Returns the reason why these settings are unusable, or null. Used to fail at startup rather than to run an
    /// "enabled" login that nobody can pass (or, worse, one that is silently open).
    /// </summary>
    public string? Validate()
    {
        if (!Enabled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            return "Admin:Username must not be empty when Admin:Enabled is true.";
        }

        if (string.IsNullOrEmpty(PasswordHash) && string.IsNullOrEmpty(Password))
        {
            return "Admin:Enabled is true but neither Admin:PasswordHash nor Admin:Password is set. "
                   + "Create a hash with `hash-password` and set Admin:PasswordHash (environment variable Admin__PasswordHash).";
        }

        if (!string.IsNullOrEmpty(PasswordHash) && !IsPasswordHasherHash(PasswordHash))
        {
            return "Admin:PasswordHash is not a valid ASP.NET Core Identity password hash. Create it with the `hash-password` command.";
        }

        if (SessionHours < 1)
        {
            return "Admin:SessionHours must be at least 1.";
        }

        return null;
    }

    private static bool IsPasswordHasherHash(string hash)
    {
        try
        {
            // Format v2 starts with 0x00 and v3 with 0x01; everything else is not a PasswordHasher payload.
            var bytes = Convert.FromBase64String(hash);
            return bytes.Length > 1 && bytes[0] is 0x00 or 0x01;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
