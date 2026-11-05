using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Domain;

/// <summary>A persisted <see cref="ValidationIssue"/>.</summary>
public class ValidationIssueRecord
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The message the issue belongs to.</summary>
    public int MessageLogId { get; set; }

    /// <summary>Navigation to <see cref="MessageLogId"/>.</summary>
    public CxmlMessageLog? MessageLog { get; set; }

    /// <summary>Severity.</summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>Machine-readable code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Line in the document, if known.</summary>
    public int? Line { get; set; }

    /// <summary>Position on the line, if known.</summary>
    public int? Position { get; set; }

    /// <summary>Where the issue came from.</summary>
    public ValidationSource Source { get; set; }

    /// <summary>Converts back to a protocol issue.</summary>
    public ValidationIssue ToIssue() => new(Severity, Code, Message, Line, Position, Source);

    /// <summary>Creates a record from a protocol issue.</summary>
    public static ValidationIssueRecord From(ValidationIssue issue) => new()
    {
        Severity = issue.Severity,
        Code = issue.Code,
        Message = issue.Message,
        Line = issue.Line,
        Position = issue.Position,
        Source = issue.Source,
    };
}
