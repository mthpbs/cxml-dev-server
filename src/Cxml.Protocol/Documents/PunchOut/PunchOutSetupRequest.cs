using System.Xml.Linq;
using Cxml.Protocol.Common;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Documents.PunchOut;

/// <summary>The buyer's request to start (or resume) a PunchOut session at the supplier.</summary>
public sealed class PunchOutSetupRequest : ICxmlPayload
{
    /// <summary>The element name, <c>PunchOutSetupRequest</c>.</summary>
    public const string Name = "PunchOutSetupRequest";

    internal static readonly CxmlPayloadCodec<PunchOutSetupRequest> Codec = new(Name, Parse, p => p.ToXml());

    /// <inheritdoc />
    public string ElementName => Name;

    /// <summary>The requested operation.</summary>
    public PunchOutOperation Operation { get; set; }

    /// <summary>
    /// The <c>BuyerCookie</c> as inner markup. Its content is <c>ANY</c> and must be returned unchanged,
    /// so XML children are preserved verbatim (text is XML-escaped as in the source document).
    /// </summary>
    public string BuyerCookie { get; set; } = string.Empty;

    /// <summary>Buyer-supplied extrinsics (e.g. <c>UserEmail</c>, <c>CostCenter</c>).</summary>
    public List<Extrinsic> Extrinsics { get; set; } = [];

    /// <summary>The URL the supplier must form-post the order message to.</summary>
    public string? BrowserFormPostUrl { get; set; }

    /// <summary>Contacts (the shopping user).</summary>
    public List<Contact> Contacts { get; set; } = [];

    /// <summary>Optional supplier setup URL (<c>SupplierSetup/URL</c>).</summary>
    public string? SupplierSetupUrl { get; set; }

    /// <summary>Default ship-to address.</summary>
    public ShipTo? ShipTo { get; set; }

    /// <summary>Item to deep-link into (<c>SelectedItem/ItemID</c>).</summary>
    public ItemId? SelectedItem { get; set; }

    /// <summary>Items of the cart being edited or inspected.</summary>
    public List<ItemOut> Items { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    private static PunchOutSetupRequest Parse(XElement e) => new()
    {
        Operation = PunchOutOperationExtensions.ParseOperation(e.Attr("operation")),
        BuyerCookie = e.Element("BuyerCookie")?.InnerXml() ?? string.Empty,
        Extrinsics = Extrinsic.ParseAll(e),
        BrowserFormPostUrl = e.Element("BrowserFormPost")?.ChildText("URL"),
        Contacts = e.Elements("Contact").Select(Contact.Parse).ToList(),
        SupplierSetupUrl = e.Element("SupplierSetup")?.ChildText("URL"),
        ShipTo = e.Element("ShipTo") is { } s ? ShipTo.Parse(s) : null,
        SelectedItem = e.Element("SelectedItem")?.Element("ItemID") is { } id ? ItemId.Parse(id) : null,
        Items = e.Elements("ItemOut").Select(ItemOut.Parse).ToList(),
        Raw = e,
    };

    private XElement ToXml()
    {
        // DTD order: BuyerCookie, Extrinsic*, BrowserFormPost?, Contact*, SupplierSetup?, ShipTo?, SelectedItem?, ItemOut*
        var e = new XElement(Name,
            new XAttribute("operation", Operation.ToCxml()),
            new XElement("BuyerCookie", XmlExtensions.ParseInnerXml(BuyerCookie)));
        e.Add(Extrinsics.Select(x => x.ToXml()));
        if (!string.IsNullOrEmpty(BrowserFormPostUrl))
        {
            e.Add(new XElement("BrowserFormPost", new XElement("URL", BrowserFormPostUrl)));
        }

        e.Add(Contacts.Select(c => c.ToXml()));
        if (!string.IsNullOrEmpty(SupplierSetupUrl))
        {
            e.Add(new XElement("SupplierSetup", new XElement("URL", SupplierSetupUrl)));
        }

        e.AddIfNotNull(ShipTo?.ToXml());
        if (SelectedItem is not null)
        {
            e.Add(new XElement("SelectedItem", SelectedItem.ToXml()));
        }

        e.Add(Items.Select(i => i.ToXml()));
        return e;
    }
}
