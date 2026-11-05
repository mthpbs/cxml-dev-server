using System.Xml.Linq;
using Cxml.Protocol.Internal;

namespace Cxml.Protocol.Common;

/// <summary>Tax summary: <c>Money, Description, TaxDetail*</c>.</summary>
public sealed class Tax
{
    /// <summary>Total tax amount.</summary>
    public Money Amount { get; set; } = new();

    /// <summary>Description of the tax.</summary>
    public Description Description { get; set; } = new();

    /// <summary>Per-category tax breakdown.</summary>
    public List<TaxDetail> Details { get; set; } = [];

    /// <summary>The source element when parsed from XML.</summary>
    public XElement? Raw { get; set; }

    internal static Tax Parse(XElement e) => new()
    {
        Amount = Money.ParseWrapped(e) ?? new Money(),
        Description = e.Element("Description") is { } d ? Common.Description.Parse(d) : new Description(),
        Details = e.Elements("TaxDetail").Select(TaxDetail.Parse).ToList(),
        Raw = e,
    };

    internal XElement ToXml()
    {
        var e = new XElement("Tax", Amount.ToXml(), Description.ToXml());
        e.Add(Details.Select(d => d.ToXml()));
        return e;
    }
}

/// <summary>One line of a tax breakdown (<c>TaxDetail</c>).</summary>
public sealed class TaxDetail
{
    /// <summary>The tax category, e.g. <c>vat</c> or <c>gst</c> (required).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Optional rate in percent.</summary>
    public string? PercentageRate { get; set; }

    /// <summary>Optional purpose, e.g. <c>tax</c> or <c>shippingTax</c>.</summary>
    public string? Purpose { get; set; }

    /// <summary>Amount the tax is computed on.</summary>
    public Money? TaxableAmount { get; set; }

    /// <summary>The tax amount.</summary>
    public Money TaxAmount { get; set; } = new();

    /// <summary>Optional location text.</summary>
    public string? TaxLocation { get; set; }

    internal static TaxDetail Parse(XElement e) => new()
    {
        Category = e.Attr("category") ?? string.Empty,
        PercentageRate = e.Attr("percentageRate"),
        Purpose = e.Attr("purpose"),
        TaxableAmount = Money.ParseWrapped(e.Element("TaxableAmount")),
        TaxAmount = Money.ParseWrapped(e.Element("TaxAmount")) ?? new Money(),
        TaxLocation = e.ChildText("TaxLocation"),
    };

    internal XElement ToXml()
    {
        var e = new XElement("TaxDetail");
        e.AddAttr("purpose", Purpose);
        e.Add(new XAttribute("category", Category));
        e.AddAttr("percentageRate", PercentageRate);
        e.AddIfNotNull(TaxableAmount?.Wrap("TaxableAmount"));
        e.Add(TaxAmount.Wrap("TaxAmount"));
        if (!string.IsNullOrEmpty(TaxLocation))
        {
            e.Add(new XElement("TaxLocation", TaxLocation));
        }

        return e;
    }
}
