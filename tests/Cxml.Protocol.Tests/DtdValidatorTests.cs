using Cxml.Protocol.Validation;

namespace Cxml.Protocol.Tests;

public class DtdValidatorTests
{
    private const string Valid = """
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE cXML SYSTEM "http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd">
        <cXML payloadID="p" timestamp="2024-01-01T00:00:00+00:00"><Response><Status code="200" text="OK"/></Response></cXML>
        """;

    private readonly CxmlDtdValidator _validator = new();

    [Fact]
    public void ValidDocument_HasNoIssues()
    {
        var result = _validator.Validate(Valid);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void MissingRequiredAttribute_IsReportedAsDtdErrorWithPosition()
    {
        var xml = Valid.Replace("payloadID=\"p\" ", string.Empty);

        var result = _validator.Validate(xml);

        Assert.False(result.IsValid);
        var issue = Assert.Single(result.Errors);
        Assert.Equal(ValidationSource.Dtd, issue.Source);
        Assert.Equal(CxmlDtdValidator.DtdErrorCode, issue.Code);
        Assert.Contains("payloadID", issue.Message);
        Assert.Equal(3, issue.Line);
        Assert.True(issue.Position > 0);
    }

    [Fact]
    public void WrongElementOrder_IsReported()
    {
        var xml = """
            <?xml version="1.0"?>
            <!DOCTYPE cXML SYSTEM "http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd">
            <cXML payloadID="p" timestamp="2024-01-01T00:00:00+00:00">
              <Header>
                <To><Credential domain="D"><Identity>1</Identity></Credential></To>
                <From><Credential domain="D"><Identity>1</Identity></Credential></From>
                <Sender><Credential domain="D"><Identity>1</Identity></Credential><UserAgent>u</UserAgent></Sender>
              </Header>
              <Request><ProfileRequest/></Request>
            </cXML>
            """;

        var result = _validator.Validate(xml);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Line == 5);
    }

    [Fact]
    public void BadEnumeratedAttribute_IsReported()
    {
        var xml = Valid.Replace("<Response>", "<Request deploymentMode=\"staging\"><ProfileRequest/></Request><Response>");

        Assert.False(_validator.Validate(xml).IsValid);
    }

    [Fact]
    public void NotWellFormed_ReportsParseError()
    {
        var result = _validator.Validate("<cXML>\n<oops></cXML>");

        var issue = Assert.Single(result.Issues);
        Assert.Equal(ValidationSource.Parse, issue.Source);
        Assert.Equal(CxmlDtdValidator.MalformedCode, issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal(2, issue.Line);
    }

    [Fact]
    public void NoDoctype_ValidatesAgainstDefaultDtdWithWarning_AndKeepsLineNumbers()
    {
        var xml = "<?xml version=\"1.0\"?>\n<cXML timestamp=\"2024-01-01T00:00:00+00:00\">\n<Response><Status code=\"200\" text=\"OK\"/></Response></cXML>";

        var result = _validator.Validate(xml);

        Assert.Contains(result.Warnings, w => w.Code == CxmlDtdValidator.NoDoctypeCode);
        var error = Assert.Single(result.Errors);
        Assert.Contains("payloadID", error.Message);
        Assert.Equal(2, error.Line);
    }

    [Fact]
    public void NoDoctypeAndNoDeclaration_StillValidates()
    {
        var xml = Valid.Split('\n')[2];

        var result = _validator.Validate(xml);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Code == CxmlDtdValidator.NoDoctypeCode);
    }

    [Fact]
    public void UnknownDtdVersion_FallsBackToCurrentWithWarning()
    {
        var xml = Valid.Replace("1.2.071", "1.2.014");

        var result = _validator.Validate(xml);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues));
        Assert.Contains(result.Warnings, w => w.Code == CxmlDtdValidator.VersionFallbackCode && w.Message.Contains("1.2.014"));
    }

    [Fact]
    public void DoctypeHostAndFileName_AreMatchedCaseInsensitively_AndHttpsIsAccepted()
    {
        var xml = Valid.Replace("http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd", "https://XML.cXML.org/schemas/cxml/1.2.071/CXML.DTD");

        var result = _validator.Validate(xml);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues));
        Assert.DoesNotContain(result.Issues, i => i.Code == CxmlDtdValidator.VersionFallbackCode);
    }

    [Theory]
    [InlineData("http://evil.example.com/cXML.dtd")]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://127.0.0.1:1/schemas/cXML/1.2.071/cXML.dtd")]
    public void ForeignDtdLocations_AreRefusedWithoutNetworkAccess(string url)
    {
        var xml = Valid.Replace("http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd", url);

        var result = _validator.Validate(xml);

        Assert.False(result.IsValid);
        Assert.Contains("not permitted", result.Errors.First().Message);
    }

    [Fact]
    public void InternalSubset_IsRejected()
    {
        var xml = Valid.Replace("cXML.dtd\">", "cXML.dtd\" [<!ENTITY a \"b\">]>");

        var result = _validator.Validate(xml);

        Assert.Contains(result.Errors, e => e.Code == CxmlDtdValidator.InternalSubsetCode);
    }

    [Fact]
    public void FulfillDtd_IsAvailable()
    {
        Assert.Contains("Fulfill.dtd", EmbeddedDtdResolver.EmbeddedFileNames);
        Assert.Contains("cXML.dtd", EmbeddedDtdResolver.EmbeddedFileNames);
    }

    [Fact]
    public void ByteOverload_Works()
    {
        Assert.True(_validator.Validate(System.Text.Encoding.UTF8.GetBytes(Valid)).IsValid);
    }
}
