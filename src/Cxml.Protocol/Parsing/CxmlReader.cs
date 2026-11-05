using System.Text;
using System.Xml;
using System.Xml.Linq;
using Cxml.Protocol.Documents;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Parsing;

/// <summary>
/// Parses cXML documents into a <see cref="CxmlEnvelope"/>. DTD processing is ignored and no external
/// resources are fetched; validation is a separate step (<see cref="Validation.CxmlDtdValidator"/>).
/// </summary>
/// <remarks>
/// Parsing is lenient: missing attributes become empty values and unknown payload elements become an
/// <see cref="UnknownPayload"/>. Only malformed XML or a non-<c>cXML</c> root throws.
/// </remarks>
public sealed class CxmlReader
{
    private readonly CxmlPayloadRegistry _registry;

    /// <summary>Creates a reader using the built-in payload parsers.</summary>
    public CxmlReader() : this(CxmlPayloadRegistry.CreateDefault()) { }

    /// <summary>Creates a reader using <paramref name="registry"/>.</summary>
    public CxmlReader(CxmlPayloadRegistry registry) => _registry = registry;

    /// <summary>Parses a cXML document from text.</summary>
    /// <exception cref="CxmlParseException">The text is not well-formed XML or not a cXML document.</exception>
    public CxmlEnvelope Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        return Load(() => SafeXml.ParseDocument(new StringReader(xml)));
    }

    /// <summary>Parses a cXML document from raw bytes (encoding detected from the XML declaration or BOM).</summary>
    /// <exception cref="CxmlParseException">The data is not well-formed XML or not a cXML document.</exception>
    public CxmlEnvelope Read(byte[] xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        return Read(new MemoryStream(xml, writable: false));
    }

    /// <summary>Parses a cXML document from a stream (encoding detected from the XML declaration or BOM).</summary>
    /// <exception cref="CxmlParseException">The data is not well-formed XML or not a cXML document.</exception>
    public CxmlEnvelope Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Load(() =>
        {
            using var reader = XmlReader.Create(stream, SafeXml.ReaderSettings());
            return XDocument.Load(reader, LoadOptions.SetLineInfo);
        });
    }

    /// <summary>Converts an already loaded document into an envelope.</summary>
    /// <exception cref="CxmlParseException">The root element is not <c>cXML</c>.</exception>
    public CxmlEnvelope Read(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var root = document.Root ?? throw new CxmlParseException("The document has no root element.");
        if (root.Name.LocalName != "cXML")
        {
            var info = (IXmlLineInfo)root;
            throw new CxmlParseException(
                $"Root element must be 'cXML' but was '{root.Name.LocalName}'.",
                info.HasLineInfo() ? info.LineNumber : 0,
                info.HasLineInfo() ? info.LinePosition : 0);
        }

        return Build(root);
    }

    private CxmlEnvelope Load(Func<XDocument> load)
    {
        XDocument document;
        try
        {
            document = load();
        }
        catch (XmlException ex)
        {
            throw new CxmlParseException(ex.Message, ex.LineNumber, ex.LinePosition, ex);
        }

        return Read(document);
    }

    private CxmlEnvelope Build(XElement root)
    {
        var timestampText = root.Attr("timestamp");
        var envelope = new CxmlEnvelope
        {
            PayloadId = root.Attr("payloadID") ?? string.Empty,
            TimestampText = timestampText,
            Timestamp = CxmlTimestamp.ParseOrNull(timestampText) ?? default,
            Lang = root.LangAttr(),
            Version = root.Attr("version"),
            Header = root.Element("Header") is { } header ? CxmlHeader.Parse(header) : null,
            Raw = root,
        };

        if (root.Element("Request") is { } request)
        {
            envelope.Request = new CxmlRequest
            {
                DeploymentMode = request.Attr("deploymentMode"),
                Payload = ParsePayload(request.Elements().FirstOrDefault()),
                Raw = request,
            };
        }
        else if (root.Element("Message") is { } message)
        {
            envelope.Message = new CxmlMessage
            {
                DeploymentMode = message.Attr("deploymentMode"),
                InReplyTo = message.Attr("inReplyTo"),
                Status = message.Element("Status") is { } ms ? Status.Parse(ms) : null,
                Payload = ParsePayload(message.Elements().FirstOrDefault(x => x.Name.LocalName != "Status")),
                Raw = message,
            };
        }
        else if (root.Element("Response") is { } response)
        {
            envelope.Response = new CxmlResponse
            {
                Status = response.Element("Status") is { } rs ? Status.Parse(rs) : new Status(),
                Payload = ParsePayload(response.Elements().FirstOrDefault(x => x.Name.LocalName != "Status")),
                Raw = response,
            };
        }

        return envelope;
    }

    private ICxmlPayload? ParsePayload(XElement? element) => element is null ? null : _registry.Parse(element);
}
