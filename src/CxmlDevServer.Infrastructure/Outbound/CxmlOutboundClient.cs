using System.Diagnostics;
using System.Text;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Parsing;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Common;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Messaging;
using CxmlDevServer.Core.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CxmlDevServer.Infrastructure.Outbound;

/// <summary><see cref="ICxmlOutboundClient"/> on top of <see cref="IHttpClientFactory"/>.</summary>
public sealed class CxmlOutboundClient : ICxmlOutboundClient
{
    /// <summary>The name of the <see cref="HttpClient"/> registration (timeout 100 seconds).</summary>
    public const string HttpClientName = "cxml-outbound";

    private static readonly UTF8Encoding Utf8 = new(false);

    private readonly IHttpClientFactory _httpClients;
    private readonly ICxmlDbContext _db;
    private readonly CxmlReader _reader;
    private readonly TimeProvider _time;
    private readonly ILogger<CxmlOutboundClient> _logger;

    /// <summary>Creates the client.</summary>
    public CxmlOutboundClient(
        IHttpClientFactory httpClients,
        ICxmlDbContext db,
        CxmlReader reader,
        TimeProvider time,
        ILogger<CxmlOutboundClient> logger)
    {
        _httpClients = httpClients;
        _db = db;
        _reader = reader;
        _time = time;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CxmlOutboundResult> SendAsync(string url, string xml, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(xml);

        var log = new CxmlMessageLog
        {
            Direction = MessageDirection.Outbound,
            Channel = MessageChannel.ManualSend,
            DocumentType = "Unknown",
            RemoteEndpoint = url,
            RawXml = xml,
            CreatedAt = _time.GetUtcNow(),
        };
        await DescribeRequestAsync(log, xml, cancellationToken);

        int? httpStatus = null;
        string? responseText = null;
        string? error = null;
        var stopwatch = Stopwatch.StartNew();
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            error = $"'{url}' is not an absolute http or https URL.";
        }
        else
        {
            try
            {
                using var content = new StringContent(xml, Utf8, "text/xml");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "UTF-8" };
                using var response = await _httpClients.CreateClient(HttpClientName).PostAsync(uri, content, cancellationToken);
                httpStatus = (int)response.StatusCode;
                log.ContentType = response.Content.Headers.ContentType?.ToString();
                responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                error = "The request timed out.";
            }
            catch (HttpRequestException ex)
            {
                error = $"The request failed: {ex.Message}";
            }
        }

        stopwatch.Stop();
        CxmlEnvelope? responseEnvelope = null;
        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                responseEnvelope = _reader.Read(responseText);
            }
            catch (CxmlParseException ex)
            {
                _logger.LogDebug(ex, "Response from {Url} is not cXML.", url);
            }
        }

        var status = responseEnvelope?.Response?.Status;
        log.HttpStatus = httpStatus;
        log.CxmlStatusCode = status?.Code;
        log.CxmlStatusText = status?.Text;
        log.ResponseXml = responseText;
        log.DurationMs = stopwatch.ElapsedMilliseconds;
        log.Notes = error;
        await CorrelateSetupAsync(log, cancellationToken);
        _db.MessageLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        return new CxmlOutboundResult(
            Succeeded: httpStatus is not null,
            httpStatus,
            responseText,
            responseEnvelope,
            status?.Code,
            status?.Text,
            stopwatch.Elapsed,
            log.Id,
            error);
    }

    /// <summary>Fills document type, payload id, partner and correlation id from the request, when it parses.</summary>
    private async Task DescribeRequestAsync(CxmlMessageLog log, string xml, CancellationToken cancellationToken)
    {
        CxmlEnvelope request;
        try
        {
            request = _reader.Read(xml);
        }
        catch (CxmlParseException)
        {
            return;
        }

        log.PayloadId = request.PayloadId;
        log.InReplyTo = request.Message?.InReplyTo;
        log.DocumentType = request.Payload?.ElementName ?? request.Kind.ToString();

        if (request.Header is { } header)
        {
            var partners = await _db.TradingPartners.AsNoTracking().ToListAsync(cancellationToken);
            log.PartnerId = TradingPartnerMatcher.Match(partners, header)?.Partner.Id;
        }

        if (request.Payload is Cxml.Protocol.Documents.Orders.OrderRequest order && !string.IsNullOrWhiteSpace(order.Header.OrderId))
        {
            log.CorrelationId = CorrelationIds.ForOrder(log.PartnerId, order.Header.OrderId);
        }
    }

    /// <summary>Links a sent PunchOutSetupRequest to the session it created (which exists only after the send).</summary>
    private async Task CorrelateSetupAsync(CxmlMessageLog log, CancellationToken cancellationToken)
    {
        if (log.DocumentType == Cxml.Protocol.Documents.PunchOut.PunchOutSetupRequest.Name && !string.IsNullOrEmpty(log.PayloadId))
        {
            log.CorrelationId = await _db.PunchOutSessions.AsNoTracking()
                .Where(s => s.SetupPayloadId == log.PayloadId)
                .Select(s => s.CorrelationId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
