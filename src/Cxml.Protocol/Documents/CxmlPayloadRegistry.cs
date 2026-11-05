using System.Collections.Concurrent;
using System.Xml.Linq;
using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Documents.Profile;
using Cxml.Protocol.Documents.PunchOut;

namespace Cxml.Protocol.Documents;

/// <summary>
/// Maps payload element names to <see cref="ICxmlPayloadParser"/> and <see cref="ICxmlPayloadWriter"/>
/// implementations. Use <see cref="CreateDefault"/> for the built-in transactions and
/// <see cref="Register(ICxmlPayloadParser)"/> to add more.
/// </summary>
public sealed class CxmlPayloadRegistry
{
    private readonly ConcurrentDictionary<string, ICxmlPayloadParser> _parsers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ICxmlPayloadWriter> _writers = new(StringComparer.Ordinal);

    /// <summary>Creates a registry containing parsers and writers for all built-in payloads.</summary>
    public static CxmlPayloadRegistry CreateDefault()
    {
        var registry = new CxmlPayloadRegistry();
        registry.Register(PunchOutSetupRequest.Codec);
        registry.Register(PunchOutSetupResponse.Codec);
        registry.Register(PunchOutOrderMessage.Codec);
        registry.Register(OrderRequest.Codec);
        registry.Register(ProfileRequest.Codec);
        registry.Register(ProfileResponse.Codec);
        return registry;
    }

    /// <summary>Element names that have a registered parser.</summary>
    public IReadOnlyCollection<string> ParserNames => _parsers.Keys.ToList();

    /// <summary>Registers (or replaces) a parser.</summary>
    public CxmlPayloadRegistry Register(ICxmlPayloadParser parser)
    {
        _parsers[parser.ElementName] = parser;
        return this;
    }

    /// <summary>Registers (or replaces) a writer.</summary>
    public CxmlPayloadRegistry Register(ICxmlPayloadWriter writer)
    {
        _writers[writer.ElementName] = writer;
        return this;
    }

    /// <summary>Registers an object that is both parser and writer.</summary>
    public CxmlPayloadRegistry Register<TCodec>(TCodec codec)
        where TCodec : ICxmlPayloadParser, ICxmlPayloadWriter
    {
        Register((ICxmlPayloadParser)codec);
        Register((ICxmlPayloadWriter)codec);
        return this;
    }

    /// <summary>Looks up the parser for a payload element name.</summary>
    public bool TryGetParser(string elementName, out ICxmlPayloadParser parser) =>
        _parsers.TryGetValue(elementName, out parser!);

    /// <summary>Looks up the writer for a payload element name.</summary>
    public bool TryGetWriter(string elementName, out ICxmlPayloadWriter writer) =>
        _writers.TryGetValue(elementName, out writer!);

    /// <summary>Parses a payload element; elements without a parser become an <see cref="UnknownPayload"/>.</summary>
    public ICxmlPayload Parse(XElement element) =>
        TryGetParser(element.Name.LocalName, out var parser) ? parser.Parse(element) : new UnknownPayload(element);

    /// <summary>Builds the XML for a payload using its registered writer.</summary>
    /// <exception cref="InvalidOperationException">No writer is registered for the payload.</exception>
    public XElement Write(ICxmlPayload payload)
    {
        if (payload is UnknownPayload unknown)
        {
            return new XElement(unknown.Raw);
        }

        return TryGetWriter(payload.ElementName, out var writer)
            ? writer.Write(payload)
            : throw new InvalidOperationException($"No cXML payload writer registered for '{payload.ElementName}'.");
    }
}
