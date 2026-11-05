using Cxml.Protocol;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Common;
using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Pipeline.Steps;

/// <summary>
/// Resolves the trading partner from the Sender (or From) credential, checks the shared secret and the
/// <c>To</c> identity. Failures are answered with 401 (unknown sender, wrong secret, wrong To in Strict mode) or
/// 403 (inactive partner).
/// </summary>
public sealed class AuthenticateStep : ICxmlPipelineStep
{
    /// <summary>Issue code: the sender matches no trading partner but unknown partners are allowed.</summary>
    public const string UnknownPartnerCode = "AUTH-UNKNOWN-PARTNER";

    /// <summary>Issue code: the To identity is not one of the supplier identities.</summary>
    public const string ToMismatchCode = "AUTH-TO-MISMATCH";

    /// <summary>Issue code: the partner was matched by its From credential because no Sender credential matched.</summary>
    public const string MatchedByFromCode = "AUTH-MATCHED-BY-FROM";

    /// <summary>Issue code: a CredentialMac was sent; it is not verified.</summary>
    public const string MacNotVerifiedCode = "AUTH-MAC-NOT-VERIFIED";

    private readonly ICxmlDbContext _db;
    private readonly SupplierIdentityService _supplierIdentities;

    /// <summary>Creates the step.</summary>
    public AuthenticateStep(ICxmlDbContext db, SupplierIdentityService supplierIdentities)
    {
        _db = db;
        _supplierIdentities = supplierIdentities;
    }

    /// <inheritdoc />
    public int Order => CxmlPipelineOrder.Authenticate;

    /// <inheritdoc />
    public async Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        var envelope = context.Envelope!;
        if (envelope.Kind == CxmlEnvelopeKind.Response)
        {
            // A bare Response carries no header; the dispatch step rejects it.
            await next();
            return;
        }

        if (envelope.Header is not { } header)
        {
            context.AddIssue(Auth(ValidationSeverity.Error, "AUTH-NO-HEADER", "The document has no Header."));
            context.Fail(CxmlStatusCodes.Unauthorized, "The document has no Header with Sender credentials.");
            return;
        }

        var partners = await _db.TradingPartners.AsNoTracking().ToListAsync(context.CancellationToken);
        var match = TradingPartnerMatcher.Match(partners, header);
        if (match is null)
        {
            if (!context.Options.AllowUnknownPartners)
            {
                var sender = header.Sender.Primary;
                context.AddIssue(Auth(ValidationSeverity.Error, "AUTH-UNKNOWN-SENDER", "The sender is not a known trading partner."));
                context.Fail(
                    CxmlStatusCodes.Unauthorized,
                    $"No trading partner matches Sender credential domain '{sender?.Domain}' identity '{sender?.Identity}'.");
                return;
            }

            context.AddIssue(Auth(
                ValidationSeverity.Warning, UnknownPartnerCode,
                "The sender is not a known trading partner; the document was processed because unknown partners are allowed."));
        }
        else
        {
            var partner = match.Partner;
            if (!partner.IsActive)
            {
                context.AddIssue(Auth(ValidationSeverity.Error, "AUTH-PARTNER-INACTIVE", $"Trading partner '{partner.Name}' is disabled."));
                context.Partner = partner;
                context.LogEntry.PartnerId = partner.Id;
                context.Fail(CxmlStatusCodes.Forbidden, $"Trading partner '{partner.Name}' is disabled.");
                return;
            }

            if (!match.MatchedBySender)
            {
                context.AddIssue(Auth(
                    ValidationSeverity.Info, MatchedByFromCode,
                    $"No Sender credential matched; the partner '{partner.Name}' was identified by its From credential."));
            }

            if (partner.AuthMode == AuthMode.SharedSecret && !SecretMatches(header, match))
            {
                context.AddIssue(Auth(ValidationSeverity.Error, "AUTH-BAD-SECRET", "The shared secret is missing or incorrect."));
                context.Partner = partner;
                context.LogEntry.PartnerId = partner.Id;
                context.Fail(CxmlStatusCodes.Unauthorized, "The SharedSecret in the Sender credential is missing or incorrect.");
                return;
            }

            if (header.Sender.Credentials.Any(c => c.CredentialMac is not null))
            {
                context.AddIssue(Auth(ValidationSeverity.Warning, MacNotVerifiedCode, "A CredentialMac was sent but is not verified by this server."));
            }

            context.Partner = partner;
            context.LogEntry.PartnerId = partner.Id;
        }

        var failure = await CheckToAsync(context, header);
        if (failure)
        {
            return;
        }

        await next();
    }

    private async Task<bool> CheckToAsync(CxmlInboundContext context, CxmlHeader header)
    {
        var mode = context.EffectiveValidationMode;
        if (mode == ValidationMode.Off)
        {
            return false;
        }

        var identities = await _supplierIdentities.GetAllAsync(context.CancellationToken);
        if (SupplierIdentityService.MatchesTo(identities, header.To, context.Partner))
        {
            return false;
        }

        var to = header.To.Primary;
        var message = $"The To credential (domain '{to?.Domain}' identity '{to?.Identity}') does not match any supplier identity of this server.";
        if (mode == ValidationMode.Strict)
        {
            context.AddIssue(Auth(ValidationSeverity.Error, ToMismatchCode, message));
            context.Fail(CxmlStatusCodes.Unauthorized, message);
            return true;
        }

        context.AddIssue(Auth(ValidationSeverity.Warning, ToMismatchCode, message));
        return false;
    }

    private static bool SecretMatches(CxmlHeader header, PartnerMatch match)
    {
        var expected = match.Partner.SharedSecret;
        var candidates = header.Sender.Credentials
            .Where(c => c.SharedSecret is not null)
            .OrderByDescending(c => ReferenceEquals(c, match.SenderCredential));
        var provided = candidates.FirstOrDefault()?.SharedSecret;
        return SecretComparer.FixedTimeEquals(expected, provided) && (provided is not null || string.IsNullOrEmpty(expected));
    }

    private static ValidationIssue Auth(ValidationSeverity severity, string code, string message) =>
        new(severity, code, message, Source: ValidationSource.Auth);
}
