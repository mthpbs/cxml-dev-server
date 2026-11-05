using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>Shipping charge: <c>Money, Description</c>.</summary>
public sealed class Shipping
{
    /// <summary>The shipping cost.</summary>
    public Money Cost { get; set; } = new();

    /// <summary>Description of the shipping method.</summary>
    public Description Description { get; set; } = new();

    /// <summary>Logistics supplier, e.g. <c>FedEx</c>.</summary>
    public string? TrackingDomain { get; set; }

    /// <summary>Tracking number.</summary>
    public string? TrackingId { get; set; }

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static Shipping Parse(XElement e) => new()
    {
        Cost = Money.ParseWrapped(e) ?? new Money(),
        Description = e.Element("Description") is { } d ? Common.Description.Parse(d) : new Description(),
        TrackingDomain = e.Attr("trackingDomain"),
        TrackingId = e.Attr("trackingId"),
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("Shipping");
        e.AddAttr("trackingDomain", TrackingDomain);
        e.AddAttr("trackingId", TrackingId);
        e.Add(Cost.ToXml(), Description.ToXml());
        return e;
    }
}
