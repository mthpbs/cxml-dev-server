using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Parsing;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Messaging;

/// <summary>A compact description of a received PunchOutOrderMessage.</summary>
/// <param name="BuyerCookie">The echoed BuyerCookie.</param>
/// <param name="InReplyTo">The payloadID of the setup request replied to.</param>
/// <param name="OperationAllowed">The announced highest operation.</param>
/// <param name="StatusCode">The message's Status code, if it has one (for example 204).</param>
/// <param name="TotalAmount">The declared total.</param>
/// <param name="Currency">The currency of the total.</param>
/// <param name="ItemCount">Number of ItemIn lines.</param>
/// <param name="CalculatedTotal">Sum of quantity times unit price over the lines.</param>
public sealed record PunchOutReturnSummary(
    string BuyerCookie,
    string? InReplyTo,
    PunchOutOperation OperationAllowed,
    int? StatusCode,
    decimal TotalAmount,
    string Currency,
    int ItemCount,
    decimal CalculatedTotal);

/// <summary>The result of receiving a browser-posted PunchOutOrderMessage.</summary>
public sealed record PunchOutReturnResult
{
    /// <summary>True when a cXML field was found and decoded.</summary>
    public bool Decoded { get; init; }

    /// <summary>The encoding that was used (<c>null</c> when nothing could be decoded).</summary>
    public PunchOutFormEncoding? Encoding { get; init; }

    /// <summary>The decoded document.</summary>
    public string? Xml { get; init; }

    /// <summary>The parsed envelope.</summary>
    public CxmlEnvelope? Envelope { get; init; }

    /// <summary>The parsed order message, when the document is one.</summary>
    public PunchOutOrderMessage? Message { get; init; }

    /// <summary>A summary of the message.</summary>
    public PunchOutReturnSummary? Summary { get; init; }

    /// <summary>All validation issues (DTD and structural).</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];

    /// <summary>The PunchOut session the message belongs to (matched by inReplyTo, else BuyerCookie).</summary>
    public PunchOutSession? MatchedSession { get; init; }

    /// <summary>The id of the message log entry.</summary>
    public int? LogId { get; init; }

    /// <summary>What went wrong when the post could not be decoded or parsed.</summary>
    public string? Error { get; init; }

    /// <summary>True when the document was decoded, parsed and has no validation errors.</summary>
    public bool IsValid => Decoded && Error is null && Issues.All(i => i.Severity != ValidationSeverity.Error);
}

/// <summary>
/// The built-in "buyer" side of PunchOut: decodes a form post, parses and validates the order message and logs it
/// (inbound, channel <c>BrowserPost</c>). Makes an end-to-end PunchOut possible without a real procurement system.
/// </summary>
public sealed class PunchOutReturnReceiver
{
    private readonly ICxmlDbContext _db;
    private readonly CxmlReader _reader;
    private readonly CxmlDtdValidator _validator;
    private readonly TimeProvider _time;

    /// <summary>Creates the receiver.</summary>
    public PunchOutReturnReceiver(ICxmlDbContext db, CxmlReader reader, CxmlDtdValidator validator, TimeProvider time)
    {
        _db = db;
        _reader = reader;
        _validator = validator;
        _time = time;
    }

    /// <summary>Handles the fields of a posted form.</summary>
    public async Task<PunchOutReturnResult> ReceiveAsync(
        IEnumerable<KeyValuePair<string, string?>> formFields,
        string? remoteEndpoint = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(formFields);
        var started = _time.GetTimestamp();
        var log = new CxmlMessageLog
        {
            Direction = MessageDirection.Inbound,
            Channel = MessageChannel.BrowserPost,
            DocumentType = PunchOutOrderMessage.Name,
            RemoteEndpoint = remoteEndpoint,
            ContentType = "application/x-www-form-urlencoded",
            CreatedAt = _time.GetUtcNow(),
        };

        string xml;
        PunchOutFormEncoding encoding;
        try
        {
            if (!PunchOutFormEncoder.TryDecode(formFields.ToList(), out xml, out encoding))
            {
                return await FinishAsync(log, started, new PunchOutReturnResult
                {
                    Error = "The form has neither a 'cXML-base64' nor a 'cXML-urlencoded' field.",
                }, cancellationToken);
            }
        }
        catch (FormatException ex)
        {
            return await FinishAsync(log, started, new PunchOutReturnResult
            {
                Error = "The 'cXML-base64' field is not valid base64: " + ex.Message,
            }, cancellationToken);
        }

        log.RawXml = xml;
        var issues = _validator.Validate(xml).Issues.ToList();
        CxmlEnvelope envelope;
        try
        {
            envelope = _reader.Read(xml);
        }
        catch (CxmlParseException ex)
        {
            return await FinishAsync(log, started, new PunchOutReturnResult
            {
                Decoded = true,
                Encoding = encoding,
                Xml = xml,
                Issues = issues,
                Error = "The document is not well-formed cXML: " + ex.Message,
            }, cancellationToken);
        }

        log.PayloadId = envelope.PayloadId;
        log.InReplyTo = envelope.Message?.InReplyTo;
        log.DocumentType = envelope.Payload?.ElementName ?? envelope.Kind.ToString();

        var message = envelope.Message?.Payload as PunchOutOrderMessage;
        if (message is null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error, "PUNCHOUT-RETURN-NOT-ORDERMESSAGE",
                "The document is not a Message with a PunchOutOrderMessage.", Source: ValidationSource.Rule));
        }

        var session = message is null ? null : await FindSessionAsync(envelope, message, cancellationToken);
        if (session is not null)
        {
            log.CorrelationId = session.CorrelationId;
            log.PartnerId = session.PartnerId;
        }

        var summary = message is null
            ? null
            : new PunchOutReturnSummary(
                message.BuyerCookie,
                envelope.Message!.InReplyTo,
                message.Header.OperationAllowed,
                envelope.Message.Status?.Code,
                message.Header.Total.Amount,
                message.Header.Total.Currency,
                message.Items.Count,
                message.Items.Sum(i => i.Quantity * i.ItemDetail.UnitPrice.Amount));

        return await FinishAsync(log, started, new PunchOutReturnResult
        {
            Decoded = true,
            Encoding = encoding,
            Xml = xml,
            Envelope = envelope,
            Message = message,
            Summary = summary,
            Issues = issues,
            MatchedSession = session,
        }, cancellationToken);
    }

    private Task<PunchOutSession?> FindSessionAsync(CxmlEnvelope envelope, PunchOutOrderMessage message, CancellationToken ct)
    {
        var inReplyTo = envelope.Message?.InReplyTo;
        var cookie = message.BuyerCookie;
        return _db.PunchOutSessions.AsNoTracking()
            .Where(s => (inReplyTo != null && s.SetupPayloadId == inReplyTo) || s.BuyerCookie == cookie)
            .OrderByDescending(s => s.SetupPayloadId == inReplyTo)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<PunchOutReturnResult> FinishAsync(
        CxmlMessageLog log, long started, PunchOutReturnResult result, CancellationToken cancellationToken)
    {
        log.DurationMs = (long)_time.GetElapsedTime(started).TotalMilliseconds;
        log.HttpStatus = 200;
        log.Notes = result.Error;
        if (result.Message is not null && result.Envelope?.Message?.Status is { } status)
        {
            log.CxmlStatusCode = status.Code;
            log.CxmlStatusText = status.Text;
        }

        var issues = result.Error is not null && result.Issues.Count == 0
            ? [new ValidationIssue(ValidationSeverity.Error, "PUNCHOUT-RETURN-UNREADABLE", result.Error, Source: ValidationSource.Parse)]
            : result.Issues;
        foreach (var issue in issues)
        {
            log.Issues.Add(ValidationIssueRecord.From(issue));
        }

        _db.MessageLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
        return result with { LogId = log.Id, Issues = issues };
    }
}
