using Cxml.Protocol.Documents;
using Cxml.Protocol.Envelope;

namespace Cxml.Protocol;

/// <summary>Helpers for building <c>Response</c> envelopes.</summary>
public static class CxmlResponses
{
    /// <summary>
    /// Builds a response envelope with a fresh payload id and the current time.
    /// </summary>
    /// <param name="code">The cXML status code.</param>
    /// <param name="text">Status text; defaults to <see cref="CxmlStatusCodes.DefaultText"/>.</param>
    /// <param name="detail">Optional detail placed in the <c>Status</c> element body.</param>
    /// <param name="payload">Optional payload; ignored (not attached) unless the code is 2xx.</param>
    /// <param name="lang">The <c>xml:lang</c> of the envelope.</param>
    public static CxmlEnvelope Status(
        int code,
        string? text = null,
        string? detail = null,
        ICxmlPayload? payload = null,
        string lang = "en-US") => new()
    {
        PayloadId = PayloadIdGenerator.Generate(),
        Timestamp = DateTimeOffset.Now,
        Lang = lang,
        Response = new CxmlResponse
        {
            Status = new Envelope.Status
            {
                Code = code,
                Text = text ?? CxmlStatusCodes.DefaultText(code),
                Detail = string.IsNullOrWhiteSpace(detail) ? null : detail,
                Lang = lang,
            },
            Payload = CxmlStatusCodes.IsSuccess(code) ? payload : null,
        },
    };

    /// <summary>A <c>200 OK</c> response, optionally with a payload.</summary>
    public static CxmlEnvelope Ok(ICxmlPayload? payload = null, string? detail = null) =>
        Status(CxmlStatusCodes.Ok, payload: payload, detail: detail);
}
