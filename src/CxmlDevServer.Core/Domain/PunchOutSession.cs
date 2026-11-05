using Cxml.Protocol.Documents.PunchOut;

namespace CxmlDevServer.Core.Domain;

/// <summary>A PunchOut shopping session started by a PunchOutSetupRequest.</summary>
public class PunchOutSession : IAuditedEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The unguessable URL-safe token that is part of the StartPage URL.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>The partner that created the session (<c>null</c> when unknown partners are allowed).</summary>
    public int? PartnerId { get; set; }

    /// <summary>Navigation to <see cref="PartnerId"/>.</summary>
    public TradingPartner? Partner { get; set; }

    /// <summary>The buyer's <c>BuyerCookie</c> as inner XML; echoed verbatim in the order message.</summary>
    public string BuyerCookie { get; set; } = string.Empty;

    /// <summary>The requested operation.</summary>
    public PunchOutOperation Operation { get; set; }

    /// <summary>Where the order message is posted (<c>BrowserFormPost/URL</c>).</summary>
    public string? BrowserFormPostUrl { get; set; }

    /// <summary>The payloadID of the setup request (becomes <c>Message@inReplyTo</c>).</summary>
    public string SetupPayloadId { get; set; } = string.Empty;

    /// <summary>The <c>deploymentMode</c> of the setup request, echoed in the order message.</summary>
    public string? DeploymentMode { get; set; }

    /// <summary>The buyer's <c>From</c> credential domain (used to address the order message).</summary>
    public string? BuyerDomain { get; set; }

    /// <summary>The buyer's <c>From</c> credential identity.</summary>
    public string? BuyerIdentity { get; set; }

    /// <summary>JSON of the request's <c>Contact</c> elements.</summary>
    public string? ContactsJson { get; set; }

    /// <summary>JSON of the request's <c>Extrinsic</c> elements.</summary>
    public string? ExtrinsicsJson { get; set; }

    /// <summary>JSON of the request's <c>ShipTo</c>.</summary>
    public string? ShipToJson { get; set; }

    /// <summary>The <c>SelectedItem</c> part number for deep links, if any.</summary>
    public string? SelectedSupplierPartId { get; set; }

    /// <summary>JSON snapshot of the cart at setup time (edit and inspect), returned on cancel.</summary>
    public string? OriginalLinesJson { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public PunchOutSessionStatus Status { get; set; } = PunchOutSessionStatus.Created;

    /// <summary>Links all log entries of this session.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>When the session expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>When the StartPage was first opened.</summary>
    public DateTimeOffset? OpenedAt { get; set; }

    /// <summary>When the cart was returned or cancelled.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>The log entry of the setup request.</summary>
    public int? SetupMessageLogId { get; set; }

    /// <summary>Navigation to <see cref="SetupMessageLogId"/>.</summary>
    public CxmlMessageLog? SetupMessageLog { get; set; }

    /// <summary>The cart.</summary>
    public List<PunchOutCartLine> Lines { get; set; } = [];

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>True when the user can still shop in this session.</summary>
    public bool IsOpenForShopping => Status is PunchOutSessionStatus.Created or PunchOutSessionStatus.Opened;

    /// <summary>True for inspect sessions, whose cart is read-only.</summary>
    public bool IsReadOnly => Operation == PunchOutOperation.Inspect;
}
