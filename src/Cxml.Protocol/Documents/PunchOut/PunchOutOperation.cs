namespace Cxml.Protocol.Documents.PunchOut;

/// <summary>PunchOut operation (<c>PunchOutSetupRequest@operation</c>) or allowed operation level.</summary>
public enum PunchOutOperation
{
    /// <summary>Missing or unrecognised value.</summary>
    Unknown = 0,
    /// <summary>Start a new shopping session.</summary>
    Create,
    /// <summary>View a previously returned cart read-only.</summary>
    Inspect,
    /// <summary>Modify a previously returned cart.</summary>
    Edit,
    /// <summary>Sourcing request (not supported by the dev server).</summary>
    Source,
}

/// <summary>Conversions between <see cref="PunchOutOperation"/> and its cXML attribute text.</summary>
public static class PunchOutOperationExtensions
{
    /// <summary>Returns the lowercase attribute value (<c>create</c>, <c>inspect</c>, <c>edit</c>, <c>source</c>).</summary>
    public static string ToCxml(this PunchOutOperation operation) => operation switch
    {
        PunchOutOperation.Create => "create",
        PunchOutOperation.Inspect => "inspect",
        PunchOutOperation.Edit => "edit",
        PunchOutOperation.Source => "source",
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Operation must be create, inspect, edit or source."),
    };

    /// <summary>Parses an attribute value; unknown text yields <see cref="PunchOutOperation.Unknown"/>.</summary>
    public static PunchOutOperation ParseOperation(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "create" => PunchOutOperation.Create,
        "inspect" => PunchOutOperation.Inspect,
        "edit" => PunchOutOperation.Edit,
        "source" => PunchOutOperation.Source,
        _ => PunchOutOperation.Unknown,
    };
}
