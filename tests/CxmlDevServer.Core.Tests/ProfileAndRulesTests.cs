using Cxml.Protocol.Documents.Profile;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Rules;
using CxmlDevServer.Core.Tests.Support;

namespace CxmlDevServer.Core.Tests;

public class ProfileAndRulesTests
{
    [Fact]
    public async Task Profile_request_lists_the_supported_transactions()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(Docs.Envelope(Docs.Request("<ProfileRequest/>")));

        Assert.Equal(200, TestApp.StatusOf(result).Code);
        var profile = Assert.IsType<ProfileResponse>(TestApp.Parse(result.ResponseText).Response!.Payload);
        Assert.Equal(
            ["OrderRequest", "ProfileRequest", "PunchOutSetupRequest"],
            profile.Transactions.Select(t => t.RequestName));
        Assert.All(profile.Transactions, t => Assert.Equal(TestApp.BaseUrl + "/cxml", t.Url));
        Assert.True(Math.Abs((DateTimeOffset.Now - profile.EffectiveDate).TotalMinutes) < 5);
        Assert.True(TestApp.Validate(result.ResponseText).IsValid);
    }

    [Fact]
    public async Task Profile_request_needs_valid_credentials_like_any_other_request()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.PostAsync(Docs.Envelope(Docs.Request("<ProfileRequest/>"), secret: "wrong"));

        Assert.Equal(401, TestApp.StatusOf(result).Code);
    }

    private static IReadOnlyList<ValidationIssue> Evaluate(IDocumentRule rule, string xml, TradingPartner? partner = null)
    {
        var context = new CxmlRuleContext(TestApp.Parse(xml), xml, partner);
        return rule.AppliesTo(context) ? rule.Evaluate(context).ToList() : [];
    }

    [Theory]
    [InlineData("2024-05-01T10:30:00+01:00", true)]
    [InlineData("2024-05-01T10:30:00-08:00", true)]
    [InlineData("2024-05-01T10:30:00Z", false)]
    [InlineData("2024-05-01T10:30:00", false)]
    [InlineData("2024-05-01T10:30:00+0100", false)]
    [InlineData("2024-05-01 10:30:00+01:00", false)]
    [InlineData("2024-13-45T10:30:00+01:00", false)]
    public void Timestamp_rule_requires_a_numeric_offset(string timestamp, bool valid)
    {
        var xml = Docs.Envelope(Docs.Request("<ProfileRequest/>"), timestamp: timestamp);

        var issues = Evaluate(new TimestampFormatRule(), xml);

        Assert.Equal(valid, issues.Count == 0);
        Assert.All(issues, i =>
        {
            Assert.Equal("RULE-TIMESTAMP-FORMAT", i.Code);
            Assert.Equal(ValidationSource.Rule, i.Source);
        });
    }

    [Fact]
    public void Order_date_rule_checks_the_order_date_format()
    {
        var good = Docs.OrderRequest();
        Assert.Empty(Evaluate(new OrderDateFormatRule(), good));

        var bad = System.Text.RegularExpressions.Regex.Replace(good, "orderDate=\"[^\"]+\"", "orderDate=\"2024-05-01\"");
        Assert.Contains(Evaluate(new OrderDateFormatRule(), bad), i => i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Currency_and_country_rules_distinguish_malformed_from_unknown_codes()
    {
        var xml = Docs.OrderRequest();

        Assert.Empty(Evaluate(new CurrencyCodeRule(), xml));
        Assert.Empty(Evaluate(new CountryCodeRule(), xml));

        var malformedCurrency = Evaluate(new CurrencyCodeRule(), xml.Replace("currency=\"USD\"", "currency=\"usd\""));
        Assert.Contains(malformedCurrency, i => i.Severity == ValidationSeverity.Error);
        var unknownCurrency = Evaluate(new CurrencyCodeRule(), xml.Replace("currency=\"USD\"", "currency=\"QQQ\""));
        Assert.All(unknownCurrency, i => Assert.Equal(ValidationSeverity.Warning, i.Severity));
        Assert.NotEmpty(unknownCurrency);

        Assert.Contains(Evaluate(new CountryCodeRule(), xml.Replace("isoCountryCode=\"US\"", "isoCountryCode=\"USA\"")),
            i => i.Severity == ValidationSeverity.Error);
        Assert.Contains(Evaluate(new CountryCodeRule(), xml.Replace("isoCountryCode=\"US\"", "isoCountryCode=\"ZZ\"")),
            i => i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Order_total_rule_accepts_tax_and_shipping_and_warns_otherwise()
    {
        Assert.Empty(Evaluate(new OrderTotalRule(), Docs.OrderRequest(total: "12.50")));
        Assert.Empty(Evaluate(new OrderTotalRule(), Docs.OrderRequest(total: "12.5")));

        var wrong = Evaluate(new OrderTotalRule(), Docs.OrderRequest(total: "13.00"));
        Assert.Single(wrong);
        Assert.Equal(ValidationSeverity.Warning, wrong[0].Severity);

        var withTax = Docs.OrderRequest(total: "13.75").Replace("</Total>",
            "</Total><Tax><Money currency=\"USD\">1.25</Money><Description xml:lang=\"en\">VAT</Description></Tax>");
        Assert.Empty(Evaluate(new OrderTotalRule(), withTax));
    }

    [Fact]
    public void Payload_id_and_buyer_cookie_rules_report_problems()
    {
        Assert.Contains(Evaluate(new PayloadIdRule(), Docs.OrderRequest(payloadId: "no-at-sign")), i => i.Severity == ValidationSeverity.Warning);
        Assert.Empty(Evaluate(new PayloadIdRule(), Docs.OrderRequest(payloadId: "1.2.3@host")));

        var setup = Docs.PunchOutSetup(buyerCookie: "  ");
        Assert.Contains(Evaluate(new BuyerCookieRule(), setup), i => i.Severity == ValidationSeverity.Error);
        Assert.Empty(Evaluate(new BuyerCookieRule(), Docs.PunchOutSetup()));

        Assert.Contains(Evaluate(new BrowserFormPostUrlRule(), Docs.PunchOutSetup(browserFormPost: "ftp://x/y")), i => i.Severity == ValidationSeverity.Error);
        Assert.Empty(Evaluate(new BrowserFormPostUrlRule(), Docs.PunchOutSetup(browserFormPost: "https://buyer.example/r?x=1")));
    }

    [Fact]
    public void Rules_do_not_apply_to_unrelated_documents()
    {
        var profile = Docs.Envelope(Docs.Request("<ProfileRequest/>"));
        foreach (IDocumentRule rule in new IDocumentRule[]
                 {
                     new OrderTotalRule(), new OrderIdRule(), new LineNumbersUniqueRule(), new BuyerCookieRule(),
                     new BrowserFormPostUrlRule(), new OrderDateFormatRule(),
                 })
        {
            Assert.Empty(Evaluate(rule, profile));
        }
    }
}
