using System.Text.RegularExpressions;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>Every <c>Money@currency</c> must be an ISO 4217 code.</summary>
public sealed partial class CurrencyCodeRule : DocumentRuleBase
{
    private static readonly HashSet<string> KnownCodes = new(
        ("AED AFN ALL AMD ANG AOA ARS AUD AWG AZN BAM BBD BDT BGN BHD BIF BMD BND BOB BRL BSD BTN BWP BYN BZD CAD CDF CHF CLP CNY COP CRC CUP "
         + "CVE CZK DJF DKK DOP DZD EGP ERN ETB EUR FJD FKP GBP GEL GHS GIP GMD GNF GTQ GYD HKD HNL HTG HUF IDR ILS INR IQD IRR ISK JMD JOD JPY "
         + "KES KGS KHR KMF KPW KRW KWD KYD KZT LAK LBP LKR LRD LSL LYD MAD MDL MGA MKD MMK MNT MOP MRU MUR MVR MWK MXN MYR MZN NAD NGN NIO NOK "
         + "NPR NZD OMR PAB PEN PGK PHP PKR PLN PYG QAR RON RSD RUB RWF SAR SBD SCR SDG SEK SGD SHP SLE SOS SRD SSP STN SVC SYP SZL THB TJS TMT "
         + "TND TOP TRY TTD TWD TZS UAH UGX USD UYU UZS VES VND VUV WST XAF XCD XOF XPF YER ZAR ZMW ZWG ZWL").Split(' '),
        StringComparer.Ordinal);

    /// <inheritdoc />
    public override string Code => "RULE-CURRENCY-CODE";

    /// <inheritdoc />
    public override string Description => "Money currency attributes must be three-letter ISO 4217 codes in upper case.";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) => context.Envelope.Raw is not null;

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var currencies = context.Envelope.Raw!.Descendants("Money")
            .Select(m => (string?)m.Attribute("currency"))
            .Where(c => c is not null)
            .Distinct();
        foreach (var currency in currencies)
        {
            if (!CodePattern().IsMatch(currency!))
            {
                yield return Error($"Currency '{currency}' is not a three-letter upper-case ISO 4217 code.");
            }
            else if (!KnownCodes.Contains(currency!))
            {
                yield return Warning($"Currency '{currency}' is not a known ISO 4217 code.");
            }
        }
    }

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
