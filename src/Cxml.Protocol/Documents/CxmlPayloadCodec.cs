using System.Xml.Linq;

namespace Cxml.Protocol.Documents;

/// <summary>
/// A delegate-based parser and writer for a payload type; the quickest way to register a new transaction.
/// </summary>
/// <typeparam name="TPayload">The payload model type.</typeparam>
public sealed class CxmlPayloadCodec<TPayload> : ICxmlPayloadParser, ICxmlPayloadWriter
    where TPayload : class, ICxmlPayload
{
    private readonly Func<XElement, TPayload> _parse;
    private readonly Func<TPayload, XElement> _write;

    /// <summary>Creates a codec for the element <paramref name="elementName"/>.</summary>
    public CxmlPayloadCodec(string elementName, Func<XElement, TPayload> parse, Func<TPayload, XElement> write)
    {
        ElementName = elementName;
        _parse = parse;
        _write = write;
    }

    /// <inheritdoc />
    public string ElementName { get; }

    /// <inheritdoc />
    public ICxmlPayload Parse(XElement element) => _parse(element);

    /// <inheritdoc />
    public XElement Write(ICxmlPayload payload) =>
        payload is TPayload typed
            ? _write(typed)
            : throw new ArgumentException($"Expected {typeof(TPayload).Name} but got {payload.GetType().Name}.", nameof(payload));
}
