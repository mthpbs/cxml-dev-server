using System.Web;
using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;

namespace CxmlDevServer.Core.Pipeline.Steps;

/// <summary>Reads the body (decoding a form-posted document if necessary), starts timing and creates the log entry.</summary>
public sealed class CaptureStep : ICxmlPipelineStep
{
    private readonly TimeProvider _time;

    /// <summary>Creates the step.</summary>
    public CaptureStep(TimeProvider time) => _time = time;

    /// <inheritdoc />
    public int Order => CxmlPipelineOrder.Capture;

    /// <inheritdoc />
    public async Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        context.LogEntry = new CxmlMessageLog
        {
            Direction = MessageDirection.Inbound,
            Channel = context.Request.Channel,
            DocumentType = "Unknown",
            RemoteEndpoint = context.RemoteEndpoint,
            ContentType = context.ContentType,
            CreatedAt = _time.GetUtcNow(),
        };

        var fields = context.Request.FormFields ?? TryParseForm(context);
        if (fields is not null && TryDecodeFormField(context, fields))
        {
            if (context.IsCompleted)
            {
                return;
            }
        }
        else
        {
            context.RawText = Common.XmlTextDecoder.Decode(context.RawBytes);
        }

        context.LogEntry.RawXml = context.RawText;
        await next();
    }

    private static IReadOnlyCollection<KeyValuePair<string, string?>>? TryParseForm(CxmlInboundContext context)
    {
        if (context.ContentType is not { } contentType
            || !contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parsed = HttpUtility.ParseQueryString(System.Text.Encoding.UTF8.GetString(context.RawBytes));
        return parsed.AllKeys.Where(k => k is not null).Select(k => new KeyValuePair<string, string?>(k!, parsed[k])).ToList();
    }

    private static bool TryDecodeFormField(CxmlInboundContext context, IReadOnlyCollection<KeyValuePair<string, string?>> fields)
    {
        try
        {
            if (!PunchOutFormEncoder.TryDecode(fields, out var xml, out var encoding))
            {
                return false;
            }

            context.RawText = xml;
            context.RawBytes = System.Text.Encoding.UTF8.GetBytes(xml);
            context.Notes = $"Document decoded from form field ({encoding}).";
            return true;
        }
        catch (FormatException ex)
        {
            context.RawText = Common.XmlTextDecoder.Decode(context.RawBytes);
            context.AddIssue(new ValidationIssue(ValidationSeverity.Error, "CXML-FORM-DECODE", ex.Message, Source: ValidationSource.Parse));
            context.Fail(406, "The form field does not contain valid base64: " + ex.Message);
            return true;
        }
    }
}
