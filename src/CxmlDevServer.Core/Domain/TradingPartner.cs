using Cxml.Protocol.Documents.PunchOut;

namespace CxmlDevServer.Core.Domain;

/// <summary>A buyer (or network) that is allowed to send documents to this supplier simulator.</summary>
public class TradingPartner : IAuditedEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Inactive partners are answered with <c>403</c>.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Domain of the <c>From</c> credential, e.g. <c>NetworkId</c>.</summary>
    public string FromDomain { get; set; } = string.Empty;

    /// <summary>Identity of the <c>From</c> credential.</summary>
    public string FromIdentity { get; set; } = string.Empty;

    /// <summary>Domain of the <c>Sender</c> credential.</summary>
    public string SenderDomain { get; set; } = string.Empty;

    /// <summary>Identity of the <c>Sender</c> credential.</summary>
    public string SenderIdentity { get; set; } = string.Empty;

    /// <summary>Optional override of the <c>To</c> credential domain this partner is expected to address.</summary>
    public string? ToDomain { get; set; }

    /// <summary>Optional override of the <c>To</c> credential identity this partner is expected to address.</summary>
    public string? ToIdentity { get; set; }

    /// <summary>The secret the partner must send in the Sender credential.</summary>
    public string? SharedSecret { get; set; }

    /// <summary>How the partner authenticates.</summary>
    public AuthMode AuthMode { get; set; } = AuthMode.SharedSecret;

    /// <summary>How strictly documents from this partner are validated.</summary>
    public ValidationMode ValidationMode { get; set; } = ValidationMode.Lenient;

    /// <summary>How PunchOut order messages are posted back to this partner.</summary>
    public PunchOutPostEncoding PunchOutPostEncoding { get; set; } = PunchOutPostEncoding.Base64;

    /// <summary>Highest PunchOut operation announced in <c>operationAllowed</c> of order messages.</summary>
    public PunchOutOperation OperationAllowed { get; set; } = PunchOutOperation.Create;

    /// <summary>Expected <c>deploymentMode</c> (<c>test</c> or <c>production</c>); <c>null</c> accepts both.</summary>
    public string? DeploymentMode { get; set; }

    /// <summary>Free-form notes.</summary>
    public string? Notes { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
