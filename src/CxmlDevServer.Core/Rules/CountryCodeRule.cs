using System.Text.RegularExpressions;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>Every <c>Country@isoCountryCode</c> must be an ISO 3166-1 alpha-2 code.</summary>
public sealed partial class CountryCodeRule : DocumentRuleBase
{
    private static readonly HashSet<string> KnownCodes = new(
        ("AD AE AF AG AI AL AM AO AQ AR AS AT AU AW AX AZ BA BB BD BE BF BG BH BI BJ BL BM BN BO BQ BR BS BT BV BW BY BZ CA CC CD CF CG CH CI CK "
         + "CL CM CN CO CR CU CV CW CX CY CZ DE DJ DK DM DO DZ EC EE EG EH ER ES ET FI FJ FK FM FO FR GA GB GD GE GF GG GH GI GL GM GN GP GQ GR "
         + "GS GT GU GW GY HK HM HN HR HT HU ID IE IL IM IN IO IQ IR IS IT JE JM JO JP KE KG KH KI KM KN KP KR KW KY KZ LA LB LC LI LK LR LS LT "
         + "LU LV LY MA MC MD ME MF MG MH MK ML MM MN MO MP MQ MR MS MT MU MV MW MX MY MZ NA NC NE NF NG NI NL NO NP NR NU NZ OM PA PE PF PG PH "
         + "PK PL PM PN PR PS PT PW PY QA RE RO RS RU RW SA SB SC SD SE SG SH SI SJ SK SL SM SN SO SR SS ST SV SX SY SZ TC TD TF TG TH TJ TK TL "
         + "TM TN TO TR TT TV TW TZ UA UG UM US UY UZ VA VC VE VG VI VN VU WF WS YE YT ZA ZM ZW").Split(' '),
        StringComparer.Ordinal);

    /// <inheritdoc />
    public override string Code => "RULE-COUNTRY-CODE";

    /// <inheritdoc />
    public override string Description => "Country isoCountryCode attributes must be two-letter upper-case ISO 3166-1 codes.";

    /// <inheritdoc />
    public override bool AppliesTo(CxmlRuleContext context) => context.Envelope.Raw is not null;

    /// <inheritdoc />
    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var codes = context.Envelope.Raw!.Descendants("Country")
            .Select(c => (string?)c.Attribute("isoCountryCode"))
            .Where(c => c is not null)
            .Distinct();
        foreach (var code in codes)
        {
            if (!CodePattern().IsMatch(code!))
            {
                yield return Error($"Country code '{code}' is not a two-letter upper-case ISO 3166-1 code.");
            }
            else if (!KnownCodes.Contains(code!))
            {
                yield return Warning($"Country code '{code}' is not an assigned ISO 3166-1 code.");
            }
        }
    }

    [GeneratedRegex("^[A-Z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
