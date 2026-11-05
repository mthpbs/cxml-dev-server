namespace CxmlDevServer.Core.Domain;

/// <summary>An identity this simulator answers to as the supplier (matched against the <c>To</c> credential).</summary>
public class SupplierIdentity : IAuditedEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Credential domain, e.g. <c>DUNS</c>.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Credential identity value.</summary>
    public string Identity { get; set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>The identity used in headers of documents this server creates (one should be the default).</summary>
    public bool IsDefault { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
