using System.Xml.Linq;

namespace Cxml.Protocol.Documents.Profile;

/// <summary>A request for the server's supported transactions. The element is empty.</summary>
public sealed class ProfileRequest : ICxmlPayload
{
    /// <summary>The element name, <c>ProfileRequest</c>.</summary>
    public const string Name = "ProfileRequest";

    internal static readonly CxmlPayloadCodec<ProfileRequest> Codec =
        new(Name, e => new ProfileRequest { Raw = e }, _ => new XElement(Name));

    /// <inheritdoc />
    public string ElementName => Name;

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }
}
