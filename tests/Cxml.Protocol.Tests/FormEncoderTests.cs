using System.Text;
using Cxml.Protocol.Documents.PunchOut;

namespace Cxml.Protocol.Tests;

public class FormEncoderTests
{
    private const string Sample = "<?xml version=\"1.0\"?><cXML a=\"x&amp;y\">café 日本 &lt;&gt; \"q\" 'a' 😀</cXML>";

    private static KeyValuePair<string, string?>[] Fields(params (string Key, string? Value)[] fields) =>
        fields.Select(f => new KeyValuePair<string, string?>(f.Key, f.Value)).ToArray();

    [Fact]
    public void Base64_RoundTripsNonAscii()
    {
        var encoded = PunchOutFormEncoder.EncodeBase64(Sample);

        Assert.Equal(Sample, Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
        Assert.Equal(Sample, PunchOutFormEncoder.Decode(Fields((PunchOutFormEncoder.Base64FieldName, encoded))));
    }

    [Fact]
    public void UrlEncodedValue_IsPureAscii_AndUsesNumericEntities()
    {
        var value = PunchOutFormEncoder.EncodeUrlEncodedFieldValue(Sample);

        Assert.All(value, c => Assert.True(c < 0x80, $"non-ASCII char U+{(int)c:X4}"));
        Assert.Contains("&#233;", value);          // é
        Assert.Contains("&#26085;&#26412;", value); // 日本
        Assert.Contains("&#128512;", value);       // surrogate pair combined into one code point
        Assert.DoesNotContain("<", value);
        Assert.DoesNotContain("\"", value);
        Assert.DoesNotContain("%", value);         // never URL-encoded by the server
    }

    [Fact]
    public void UrlEncodedValue_RoundTripsThroughHtmlDecode()
    {
        var value = PunchOutFormEncoder.EncodeUrlEncodedFieldValue(Sample);

        // The browser HTML-decodes the attribute value, which is what the buyer then receives.
        Assert.Equal(Sample, PunchOutFormEncoder.DecodeUrlEncodedFieldValue(value));
        Assert.Equal(Sample, PunchOutFormEncoder.Decode(Fields((PunchOutFormEncoder.UrlEncodedFieldName, Sample))));
    }

    [Theory]
    [InlineData("&<>\"")]
    [InlineData("a & b < c > d \" e ' f")]
    [InlineData("é日本")]
    public void SpecialCharacters_SurviveBothEncodings(string text)
    {
        var xml = $"<x>{text.Replace("&", "&amp;").Replace("<", "&lt;")}</x>";

        Assert.Equal(xml, PunchOutFormEncoder.DecodeUrlEncodedFieldValue(PunchOutFormEncoder.EncodeUrlEncodedFieldValue(xml)));
        Assert.Equal(xml, PunchOutFormEncoder.Decode(Fields(("cXML-base64", PunchOutFormEncoder.EncodeBase64(xml)))));
    }

    [Fact]
    public void Decode_PrefersBase64OverUrlEncoded_AndIgnoresFieldNameCase()
    {
        var fields = Fields(("CXML-URLENCODED", "<urlencoded/>"), ("Cxml-Base64", PunchOutFormEncoder.EncodeBase64("<base64/>")));

        Assert.True(PunchOutFormEncoder.TryDecode(fields, out var xml, out var encoding));
        Assert.Equal("<base64/>", xml);
        Assert.Equal(PunchOutFormEncoding.Base64, encoding);
    }

    [Fact]
    public void Decode_FallsBackToUrlEncoded()
    {
        Assert.True(PunchOutFormEncoder.TryDecode(Fields(("cXML-urlencoded", "<a/>")), out var xml, out var encoding));
        Assert.Equal("<a/>", xml);
        Assert.Equal(PunchOutFormEncoding.UrlEncoded, encoding);
    }

    [Fact]
    public void Decode_WithoutKnownField_ReturnsNull()
    {
        Assert.Null(PunchOutFormEncoder.Decode(Fields(("other", "x"))));
        Assert.False(PunchOutFormEncoder.TryDecode(Fields(), out _, out _));
    }

    [Fact]
    public void Decode_ToleratesPlusTurnedIntoSpaceAndLineBreaks()
    {
        // 0xFB 0xEF 0xBE encodes to "++++"; naive form decoding turns '+' into ' '.
        var xml = "<a>" + new string('~', 40) + "?></a>";
        var base64 = PunchOutFormEncoder.EncodeBase64(xml);
        var mangled = base64.Replace('+', ' ').Insert(8, "\r\n");

        Assert.Equal(xml, PunchOutFormEncoder.Decode(Fields(("cXML-base64", mangled))));
    }

    [Fact]
    public void Decode_StripsByteOrderMark()
    {
        var withBom = Convert.ToBase64String([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("<a/>")]);

        Assert.Equal("<a/>", PunchOutFormEncoder.Decode(Fields(("cXML-base64", withBom))));
    }

    [Fact]
    public void Encode_ReturnsFieldNameAndValue()
    {
        Assert.Equal("cXML-base64", PunchOutFormEncoder.Encode("<a/>", PunchOutFormEncoding.Base64).Key);
        Assert.Equal("cXML-urlencoded", PunchOutFormEncoder.Encode("<a/>", PunchOutFormEncoding.UrlEncoded).Key);
    }
}
