namespace CxmlDevServer.Core.Domain;

/// <summary>A configured override of how the server answers matching inbound requests (for testing buyer error handling).</summary>
public class ResponseScenario : IAuditedEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Disabled scenarios never match.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Only requests from this partner match; <c>null</c> matches any partner.</summary>
    public int? PartnerId { get; set; }

    /// <summary>Navigation to <see cref="PartnerId"/>.</summary>
    public TradingPartner? Partner { get; set; }

    /// <summary>Only this payload element (e.g. <c>OrderRequest</c>) matches; <c>null</c> matches any.</summary>
    public string? DocumentType { get; set; }

    /// <summary>Only requests whose raw XML contains this text match.</summary>
    public string? MatchContains { get; set; }

    /// <summary>The highest priority wins when several scenarios match.</summary>
    public int Priority { get; set; }

    /// <summary>How many more times the scenario may fire; <c>null</c> is unlimited. At zero it no longer matches.</summary>
    public int? RemainingUses { get; set; }

    /// <summary>What the scenario does.</summary>
    public ScenarioBehavior Behavior { get; set; } = ScenarioBehavior.Respond;

    /// <summary>cXML status code to answer with (defaults to 500).</summary>
    public int? StatusCode { get; set; }

    /// <summary>cXML status text (defaults to the standard text of the code).</summary>
    public string? StatusText { get; set; }

    /// <summary>Text for the body of the <c>Status</c> element.</summary>
    public string? StatusDetail { get; set; }

    /// <summary>HTTP status to return; <c>null</c> keeps the default of 200.</summary>
    public int? HttpStatus { get; set; }

    /// <summary>Artificial delay before answering, in milliseconds.</summary>
    public int DelayMs { get; set; }

    /// <summary>Body used for <see cref="ScenarioBehavior.MalformedBody"/>; a truncated cXML document when empty.</summary>
    public string? MalformedBody { get; set; }

    /// <summary>How many times the scenario fired.</summary>
    public int HitCount { get; set; }

    /// <summary>When the scenario last fired.</summary>
    public DateTimeOffset? LastHitAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
