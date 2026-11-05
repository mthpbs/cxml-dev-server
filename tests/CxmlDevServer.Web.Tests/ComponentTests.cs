using Bunit;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Web.Components.Shared;
using CxmlDevServer.Web.Services;

namespace CxmlDevServer.Web.Tests;

public sealed class ComponentTests : BunitContext
{
    private static PunchOutCartLine Line(int id, string name, decimal qty, decimal price) => new()
    {
        Id = id, LineNumber = id, SupplierPartId = "P-" + id, ShortName = name, Quantity = qty, UnitPrice = price, Currency = "USD",
    };

    [Fact]
    public void Cart_panel_shows_lines_and_the_total()
    {
        var cut = Render<CartPanel>(p => p.Add(c => c.Lines, [Line(1, "Pens", 2, 5m), Line(2, "Chair", 1, 289m)]));

        Assert.Equal("2 lines", cut.Find("#cart-count").TextContent);
        Assert.Equal("299.00 USD", cut.Find("#cart-total").TextContent);
        Assert.Equal(2, cut.FindAll("[data-line-id]").Count);
    }

    [Fact]
    public void Cart_panel_reports_quantity_changes_and_removals()
    {
        (int LineId, decimal Quantity)? changed = null;
        int? removed = null;
        var cut = Render<CartPanel>(p => p
            .Add(c => c.Lines, [Line(7, "Pens", 2, 5m)])
            .Add(c => c.OnQuantityChanged, e => changed = e)
            .Add(c => c.OnRemove, id => removed = id));

        cut.Find("input.qty-input").Change("4");
        cut.Find("button.btn-link").Click();

        Assert.Equal((7, 4m), changed);
        Assert.Equal(7, removed);
    }

    [Fact]
    public void Read_only_cart_has_no_inputs_and_an_empty_cart_says_so()
    {
        var readOnly = Render<CartPanel>(p => p.Add(c => c.Lines, [Line(1, "Pens", 2, 5m)]).Add(c => c.ReadOnly, true));
        Assert.Empty(readOnly.FindAll("input"));
        Assert.Contains("read-only", readOnly.Markup);

        var empty = Render<CartPanel>(p => p.Add(c => c.Lines, []));
        Assert.NotNull(empty.Find("#cart-empty"));
    }

    [Fact]
    public void Product_card_adds_the_chosen_quantity()
    {
        var item = new CatalogItem { Id = 3, SupplierPartId = "X-1", ShortName = "Widget", Description = "A widget", UnitPrice = 9.5m, Currency = "USD", Category = "Parts" };
        (CatalogItem Item, decimal Quantity)? added = null;
        var cut = Render<ProductCard>(p => p.Add(c => c.Item, item).Add(c => c.OnAdd, e => added = e));

        cut.Find("input[type=number]").Change("3");
        cut.Find("button").Click();

        Assert.Equal(3, added?.Item.Id);
        Assert.Equal(3m, added?.Quantity);
    }

    [Theory]
    [InlineData(200, "text-bg-success")]
    [InlineData(406, "text-bg-warning")]
    [InlineData(500, "text-bg-danger")]
    public void Status_badge_colors_follow_the_status_class(int code, string expectedClass)
    {
        var cut = Render<StatusBadge>(p => p.Add(c => c.Code, code));

        Assert.Contains(expectedClass, cut.Find("span.badge").ClassName);
    }

    [Fact]
    public void Xml_formatter_indents_valid_xml_and_returns_broken_xml_unchanged()
    {
        Assert.Contains(Environment.NewLine + "  <b>", XmlFormatter.Pretty("<a><b>x</b></a>"));
        Assert.Equal("<a><b>", XmlFormatter.Pretty("<a><b>"));
        Assert.Equal(string.Empty, XmlFormatter.Pretty(null));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Xml_formatter_keeps_the_xml_declaration_and_doctype_exactly(string newline)
    {
        const string declaration = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
        const string doctype = "<!DOCTYPE cXML SYSTEM \"http://xml.cxml.org/schemas/cXML/1.2.071/Fulfill.dtd\">";
        var raw = declaration + newline + doctype + newline + "<cXML><Request><a>x</a></Request></cXML>";

        var pretty = XmlFormatter.Pretty(raw);

        Assert.StartsWith(declaration + newline + doctype + Environment.NewLine + "<cXML>", pretty);
        Assert.Contains(Environment.NewLine + "  <Request>", pretty);
    }

    [Fact]
    public void Xml_formatter_keeps_a_doctype_that_has_a_public_id()
    {
        const string doctype = "<!DOCTYPE cXML PUBLIC \"-//cXML//DTD\" \"http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd\">";

        var pretty = XmlFormatter.Pretty(doctype + "<cXML><a/></cXML>");

        Assert.StartsWith(doctype + Environment.NewLine + "<cXML>", pretty);
    }
}
