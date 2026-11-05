using Cxml.Protocol.Parsing;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Pipeline.Steps;

/// <summary>Parses the document; anything that is not well-formed cXML is answered with 406.</summary>
public sealed class ParseStep : ICxmlPipelineStep
{
    /// <summary>The issue code reported for documents that cannot be parsed.</summary>
    public const string ParseErrorCode = "CXML-PARSE";

    private readonly CxmlReader _reader;

    /// <summary>Creates the step.</summary>
    public ParseStep(CxmlReader reader) => _reader = reader;

    /// <inheritdoc />
    public int Order => CxmlPipelineOrder.Parse;

    /// <inheritdoc />
    public async Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        if (string.IsNullOrWhiteSpace(context.RawText))
        {
            context.AddIssue(new ValidationIssue(ValidationSeverity.Error, ParseErrorCode, "The request body is empty.", Source: ValidationSource.Parse));
            context.Fail(406, "The request body is empty; expected a cXML document.");
            return;
        }

        try
        {
            var envelope = _reader.Read(context.RawText);
            context.Envelope = envelope;
            context.DocumentType = envelope.Payload?.ElementName ?? envelope.Kind.ToString();
            var log = context.LogEntry;
            log.DocumentType = context.DocumentType;
            log.PayloadId = envelope.PayloadId;
            log.InReplyTo = envelope.Message?.InReplyTo;
        }
        catch (CxmlParseException ex)
        {
            context.AddIssue(new ValidationIssue(
                ValidationSeverity.Error, ParseErrorCode, ex.Detail,
                ex.Line > 0 ? ex.Line : null, ex.Line > 0 ? ex.Position : null, ValidationSource.Parse));
            var where = ex.Line > 0 ? $" (line {ex.Line}, position {ex.Position})" : string.Empty;
            context.Fail(406, $"The request is not a well-formed cXML document: {ex.Detail}{where}");
            return;
        }

        await next();
    }
}
