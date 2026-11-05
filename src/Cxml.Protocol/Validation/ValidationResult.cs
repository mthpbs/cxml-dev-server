namespace Cxml.Protocol.Validation;

/// <summary>The outcome of validating a document: an ordered list of issues.</summary>
public sealed class ValidationResult
{
    /// <summary>Creates a result from a set of issues.</summary>
    public ValidationResult(IEnumerable<ValidationIssue>? issues = null) =>
        Issues = (issues ?? []).ToList();

    /// <summary>All issues, in the order found.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    /// <summary>True when there are no <see cref="ValidationSeverity.Error"/> issues (warnings are allowed).</summary>
    public bool IsValid => !Issues.Any(i => i.Severity == ValidationSeverity.Error);

    /// <summary>The error issues.</summary>
    public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == ValidationSeverity.Error);

    /// <summary>The warning issues.</summary>
    public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == ValidationSeverity.Warning);

    /// <summary>Returns a new result containing these issues followed by <paramref name="more"/>.</summary>
    public ValidationResult Merge(IEnumerable<ValidationIssue> more) => new(Issues.Concat(more));
}
