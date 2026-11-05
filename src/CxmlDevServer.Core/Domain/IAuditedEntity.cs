namespace CxmlDevServer.Core.Domain;

/// <summary>An entity with creation and modification timestamps maintained by the database context.</summary>
public interface IAuditedEntity
{
    /// <summary>When the row was created.</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the row was last modified.</summary>
    DateTimeOffset UpdatedAt { get; set; }
}
