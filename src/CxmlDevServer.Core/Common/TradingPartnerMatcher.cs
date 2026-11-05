using Cxml.Protocol.Envelope;
using CxmlDevServer.Core.Domain;

namespace CxmlDevServer.Core.Common;

/// <summary>The outcome of matching a document header against the trading partners.</summary>
/// <param name="Partner">The partner found.</param>
/// <param name="MatchedBySender">True when the Sender credential matched; false when only the From credential did.</param>
/// <param name="SenderCredential">The Sender credential to take the shared secret from (the matching one when available).</param>
public sealed record PartnerMatch(TradingPartner Partner, bool MatchedBySender, Credential? SenderCredential);

/// <summary>Finds the trading partner a header belongs to.</summary>
public static class TradingPartnerMatcher
{
    /// <summary>
    /// Matches by Sender credential first (preferring a partner whose From credential matches as well, because
    /// networks send for many buyers); if no Sender matches, by From credential (direct connections).
    /// </summary>
    public static PartnerMatch? Match(IReadOnlyList<TradingPartner> partners, CxmlHeader header)
    {
        ArgumentNullException.ThrowIfNull(partners);
        ArgumentNullException.ThrowIfNull(header);

        var bySender = partners
            .Where(p => FindCredential(header.Sender, p.SenderDomain, p.SenderIdentity) is not null)
            .OrderByDescending(p => FindCredential(header.From, p.FromDomain, p.FromIdentity) is not null)
            .ThenByDescending(p => p.IsActive)
            .ThenBy(p => p.Id)
            .FirstOrDefault();
        if (bySender is not null)
        {
            return new PartnerMatch(bySender, true, FindCredential(header.Sender, bySender.SenderDomain, bySender.SenderIdentity));
        }

        var byFrom = partners
            .Where(p => FindCredential(header.From, p.FromDomain, p.FromIdentity) is not null)
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Id)
            .FirstOrDefault();
        return byFrom is null
            ? null
            : new PartnerMatch(byFrom, false, header.Sender.Credentials.FirstOrDefault(c => c.SharedSecret is not null) ?? header.Sender.Primary);
    }

    /// <summary>Finds the credential with the given domain and identity (case-insensitive).</summary>
    public static Credential? FindCredential(Party party, string domain, string identity) =>
        string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(identity)
            ? null
            : party.Credentials.FirstOrDefault(c => IsSame(c, domain, identity));

    /// <summary>True when the credential has the given domain and identity (case-insensitive, trimmed).</summary>
    public static bool IsSame(Credential credential, string domain, string identity) =>
        string.Equals(credential.Domain.Trim(), domain.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(credential.Identity.Trim(), identity.Trim(), StringComparison.OrdinalIgnoreCase);
}
