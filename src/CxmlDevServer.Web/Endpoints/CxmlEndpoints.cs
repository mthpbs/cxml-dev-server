using Cxml.Protocol;
using Cxml.Protocol.Writing;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Pipeline;

namespace CxmlDevServer.Web.Endpoints;

/// <summary>The cXML endpoint: <c>POST /cxml</c> receives documents, <c>GET /cxml</c> answers a ping.</summary>
public static class CxmlEndpoints
{
    /// <summary>Maps <c>/cxml</c>.</summary>
    public static IEndpointRouteBuilder MapCxmlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/cxml", async (HttpContext context, CxmlWriter writer) =>
        {
            var bytes = writer.WriteBytes(CxmlResponses.Ok(detail: "cXML Dev Server is running."));
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = CxmlVersion.ContentType;
            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
        }).ExcludeFromDescription();

        app.MapPost("/cxml", HandlePostAsync)
            .DisableAntiforgery()
            .ExcludeFromDescription();
        return app;
    }

    private static async Task HandlePostAsync(HttpContext context, CxmlInboundProcessor processor)
    {
        var request = context.Request;
        var cancellationToken = context.RequestAborted;

        byte[] body = [];
        IReadOnlyCollection<KeyValuePair<string, string?>>? formFields = null;
        if (request.HasFormContentType && request.ContentType!.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
        {
            var form = await request.ReadFormAsync(cancellationToken);
            formFields = form.SelectMany(f => f.Value.Select(v => new KeyValuePair<string, string?>(f.Key, v))).ToList();
        }
        else
        {
            // The raw body is passed on for every other content type: the pipeline itself decodes an
            // application/x-www-form-urlencoded body (cXML-base64 / cXML-urlencoded field) and otherwise treats
            // the body as the document, so a cXML document sent with a form content type by mistake still works.
            using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer, cancellationToken);
            body = buffer.ToArray();
        }

        var result = await processor.ProcessAsync(new CxmlInboundRequest
        {
            Body = body,
            ContentType = request.ContentType,
            FormFields = formFields,
            RemoteEndpoint = context.Connection.RemoteIpAddress is { } ip ? $"{ip}:{context.Connection.RemotePort}" : null,
            BaseUrl = BaseUrl(request),
            Channel = MessageChannel.Http,
        }, cancellationToken);

        if (result.Abort)
        {
            // Simulates a server that never answers: drop the connection without a response.
            context.Abort();
            return;
        }

        context.Response.StatusCode = result.HttpStatus;
        context.Response.ContentType = result.ContentType;
        context.Response.ContentLength = result.ResponseBytes.Length;
        await context.Response.Body.WriteAsync(result.ResponseBytes, cancellationToken);
    }

    /// <summary>Scheme, host and path base of the request, without a trailing slash.</summary>
    internal static string BaseUrl(HttpRequest request) => $"{request.Scheme}://{request.Host}{request.PathBase}";
}
