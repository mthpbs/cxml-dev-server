namespace Cxml.Protocol.Validation;

/// <summary>How serious a <see cref="ValidationIssue"/> is.</summary>
public enum ValidationSeverity
{
    /// <summary>Informational note.</summary>
    Info,
    /// <summary>Questionable but accepted.</summary>
    Warning,
    /// <summary>The document is invalid.</summary>
    Error,
}

/// <summary>Which stage of processing produced a <see cref="ValidationIssue"/>.</summary>
public enum ValidationSource
{
    /// <summary>DTD validation.</summary>
    Dtd,
    /// <summary>A business rule (checks the DTD cannot express).</summary>
    Rule,
    /// <summary>Authentication or authorisation.</summary>
    Auth,
    /// <summary>XML parsing (well-formedness).</summary>
    Parse,
}

/// <summary>A single finding produced while validating a cXML document.</summary>
/// <param name="Severity">How serious the finding is.</param>
/// <param name="Code">A stable machine-readable code, e.g. <c>CXML-DTD</c>.</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="Line">1-based line in the source document, if known.</param>
/// <param name="Position">1-based column in the source document, if known.</param>
/// <param name="Source">Which stage raised it.</param>
public sealed record ValidationIssue(
    ValidationSeverity Severity,
    string Code,
    string Message,
    int? Line = null,
    int? Position = null,
    ValidationSource Source = ValidationSource.Dtd)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"{Severity} {Code}: {Message}" + (Line is { } l ? $" (line {l}, position {Position})" : string.Empty);
}
