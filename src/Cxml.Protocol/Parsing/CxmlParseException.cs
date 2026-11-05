namespace Cxml.Protocol.Parsing;

/// <summary>Thrown when a document is not well-formed XML or is not a cXML document at all.</summary>
public sealed class CxmlParseException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What went wrong (without position).</param>
    /// <param name="line">1-based line, or 0 when unknown.</param>
    /// <param name="position">1-based column, or 0 when unknown.</param>
    /// <param name="innerException">The underlying XML error, if any.</param>
    public CxmlParseException(string message, int line = 0, int position = 0, Exception? innerException = null)
        : base(line > 0 ? $"{message} (line {line}, position {position})" : message, innerException)
    {
        Detail = message;
        Line = line;
        Position = position;
    }

    /// <summary>The error description without the position suffix.</summary>
    public string Detail { get; }

    /// <summary>1-based line of the error, or 0 when unknown.</summary>
    public int Line { get; }

    /// <summary>1-based column of the error, or 0 when unknown.</summary>
    public int Position { get; }
}
