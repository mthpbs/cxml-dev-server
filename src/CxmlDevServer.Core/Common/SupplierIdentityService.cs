using Cxml.Protocol.Envelope;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CxmlOptions = CxmlDevServer.Core.Options.CxmlServerOptions;

namespace CxmlDevServer.Core.Common;

/// <summary>Resolves the identities this server answers to as the supplier.</summary>
public sealed class SupplierIdentityService
{
    private readonly ICxmlDbContext _db;
    private readonly CxmlOptions _options;

    /// <summary>Creates the service.</summary>
    public SupplierIdentityService(ICxmlDbContext db, IOptions<CxmlOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    /// <summary>All supplier identities; the configured fallback identity when none is stored.</summary>
    public async Task<IReadOnlyList<SupplierIdentity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await _db.SupplierIdentities.AsNoTracking()
            .OrderByDescending(i => i.IsDefault).ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);
        return all.Count > 0 ? all : [Fallback()];
    }

    /// <summary>The identity used in headers of documents this server creates.</summary>
    public async Task<SupplierIdentity> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken))[0];

    /// <summary>
    /// True when one of the <c>To</c> credentials is a supplier identity or the partner's <c>To</c> override.
    /// </summary>
    public static bool MatchesTo(IEnumerable<SupplierIdentity> identities, Party to, TradingPartner? partner)
    {
        foreach (var credential in to.Credentials)
        {
            if (identities.Any(i => TradingPartnerMatcher.IsSame(credential, i.Domain, i.Identity)))
            {
                return true;
            }

            if (partner is { ToDomain: { Length: > 0 } d, ToIdentity: { Length: > 0 } id }
                && TradingPartnerMatcher.IsSame(credential, d, id))
            {
                return true;
            }
        }

        return false;
    }

    private SupplierIdentity Fallback() => new()
    {
        Domain = _options.SupplierDomain,
        Identity = _options.SupplierIdentity,
        Name = "Configured default",
        IsDefault = true,
    };
}
