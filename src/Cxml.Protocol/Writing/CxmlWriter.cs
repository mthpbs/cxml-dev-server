using System.Text;
using System.Xml;
using System.Xml.Linq;
using Cxml.Protocol.Documents;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Writing;

/// <summary>
/// Serialises a <see cref="CxmlEnvelope"/> to a UTF-8 cXML document with XML declaration and DOCTYPE.
/// Element order follows the cXML DTD content models.
/// </summary>
public sealed class CxmlWriter
{
    private const string Declaration = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly CxmlPayloadRegistry _registry;
    private readonly CxmlWriterOptions _options;

    /// <summary>Creates a writer with the built-in payload writers and default options.</summary>
    public CxmlWriter() : this(CxmlPayloadRegistry.CreateDefault()) { }

    /// <summary>Creates a writer using <paramref name="registry"/>.</summary>
    public CxmlWriter(CxmlPayloadRegistry registry, CxmlWriterOptions? options = null)
    {
        _registry = registry;
        _options = options ?? new CxmlWriterOptions();
    }

    /// <summary>
    /// Writes the envelope as a string that starts with <c>&lt;?xml</c> (no BOM, no leading whitespace),
    /// followed by the DOCTYPE and the <c>cXML</c> element.
    /// </summary>
    public string Write(CxmlEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var root = ToElement(envelope);

        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            Indent = _options.Indent,
            NewLineHandling = NewLineHandling.Entitize,
            NewLineChars = "\n",
        };

        var builder = new StringBuilder();
        builder.Append(Declaration).Append('\n');
        builder.Append("<!DOCTYPE cXML SYSTEM \"").Append(_options.DtdUrl).Append("\">").Append('\n');
        using (var writer = XmlWriter.Create(builder, settings))
        {
            root.WriteTo(writer);
        }

        return builder.ToString();
    }

    /// <summary>Writes the envelope as UTF-8 bytes without a byte-order mark.</summary>
    public byte[] WriteBytes(CxmlEnvelope envelope) => Utf8NoBom.GetBytes(Write(envelope));

    /// <summary>Builds the <c>cXML</c> root element for an envelope.</summary>
    public XElement ToElement(CxmlEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var root = new XElement("cXML");
        root.AddAttr("version", envelope.Version);
        root.Add(new XAttribute("payloadID", envelope.PayloadId));
        root.Add(new XAttribute("timestamp", CxmlTimestamp.Format(envelope.Timestamp)));
        root.AddAttr(XmlNames.Lang, envelope.Lang);

        // cXML content model: ((Header, (Message | Request)) | Response)
        if (envelope.Request is not null)
        {
            root.Add(RequireHeader(envelope).ToXml(), WriteRequest(envelope.Request));
        }
        else if (envelope.Message is not null)
        {
            root.Add(RequireHeader(envelope).ToXml(), WriteMessage(envelope.Message));
        }
        else if (envelope.Response is not null)
        {
            root.Add(WriteResponse(envelope.Response));
        }
        else
        {
            throw new InvalidOperationException("The envelope has no Request, Message or Response.");
        }

        return root;
    }

    private static CxmlHeader RequireHeader(CxmlEnvelope envelope) =>
        envelope.Header ?? throw new InvalidOperationException("Request and Message envelopes require a Header.");

    private XElement WriteRequest(CxmlRequest request)
    {
        var e = new XElement("Request");
        e.AddAttr("deploymentMode", request.DeploymentMode);
        if (request.Payload is not null)
        {
            e.Add(_registry.Write(request.Payload));
        }

        return e;
    }

    private XElement WriteMessage(CxmlMessage message)
    {
        var e = new XElement("Message");
        e.AddAttr("deploymentMode", message.DeploymentMode);
        e.AddAttr("inReplyTo", message.InReplyTo);
        e.AddIfNotNull(message.Status?.ToXml());
        if (message.Payload is not null)
        {
            e.Add(_registry.Write(message.Payload));
        }

        return e;
    }

    private XElement WriteResponse(CxmlResponse response)
    {
        var e = new XElement("Response", response.Status.ToXml());
        if (response.Payload is not null)
        {
            e.Add(_registry.Write(response.Payload));
        }

        return e;
    }
}
