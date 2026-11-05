namespace CxmlDevServer.Core.PunchOut;

/// <summary>Why a PunchOut operation was refused.</summary>
public enum PunchOutError
{
    /// <summary>No session exists for the token.</summary>
    NotFound,

    /// <summary>The session expired.</summary>
    Expired,

    /// <summary>The session was already submitted or cancelled.</summary>
    Completed,

    /// <summary>The cart of an inspect session cannot be changed or returned.</summary>
    ReadOnly,

    /// <summary>The catalog item or cart line does not exist.</summary>
    ItemNotFound,

    /// <summary>The request is invalid (for example a non-positive quantity).</summary>
    Invalid,
}

/// <summary>Thrown by <see cref="PunchOutService"/> when an operation is not allowed in the session's state.</summary>
public sealed class PunchOutSessionException : Exception
{
    /// <summary>Creates the exception.</summary>
    public PunchOutSessionException(PunchOutError error, string message)
        : base(message) => Error = error;

    /// <summary>The reason.</summary>
    public PunchOutError Error { get; }
}
