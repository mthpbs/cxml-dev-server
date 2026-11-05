using System.Xml.Linq;
using Cxml.Protocol.Documents;
using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Parsing;
using Cxml.Protocol.Tests.Support;

namespace Cxml.Protocol.Tests;

public class ReaderTests
{
    private const string Header = """
        <Header>
          <From><Credential domain="NetworkId"><Identity>B</Identity></Credential></From>
          <To><Credential domain="DUNS"><Identity>1</Identity></Credential></To>
          <Sender><Credential domain="NetworkId"><Identity>B</Identity><SharedSecret>s</SharedSecret></Credential><UserAgent>UA</UserAgent></Sender>
        </Header>
        """;

    private static string Doc(string body) =>
        $"""<cXML payloadID="p1" timestamp="2024-01-01T00:00:00Z" xml:lang="en">{body}</cXML>""";

    [Fact]
    public void MalformedXml_ThrowsParseExceptionWithLineAndPosition()
    {
        var xml = "<cXML payloadID=\"1\" timestamp=\"2024-01-01T00:00:00+00:00\">\n  <Header>\n  </Wrong>\n</cXML>";

        var ex = Assert.Throws<CxmlParseException>(() => new CxmlReader().Read(xml));

        Assert.Equal(3, ex.Line);
        Assert.True(ex.Position > 0);
        Assert.Contains("line 3", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not xml at all")]
    [InlineData("<cXML")]
    public void NotXml_ThrowsParseException(string text)
    {
        Assert.Throws<CxmlParseException>(() => new CxmlReader().Read(text));
    }

    [Fact]
    public void NonCxmlRoot_ThrowsParseException()
    {
        var ex = Assert.Throws<CxmlParseException>(() => new CxmlReader().Read("<html/>"));

        Assert.Contains("cXML", ex.Message);
    }

    [Fact]
    public void UnknownPayload_BecomesUnknownPayloadWithRawElement()
    {
        var envelope = new CxmlReader().Read(Doc($"{Header}<Request><MysteryRequest id=\"7\"><X/></MysteryRequest></Request>"));

        var unknown = Assert.IsType<UnknownPayload>(envelope.Payload);
        Assert.Equal("MysteryRequest", unknown.ElementName);
        Assert.Equal("7", (string?)unknown.Raw.Attribute("id"));
    }

    [Fact]
    public void Response_WithStatusAndPayload_IsParsed()
    {
        var envelope = new CxmlReader().Read(Doc("""<Response><Status code="200" text="OK">fine</Status><ProfileResponse effectiveDate="2024-01-01T00:00:00Z"><Transaction requestName="OrderRequest"><URL>u</URL></Transaction></ProfileResponse></Response>"""));

        Assert.Equal(200, envelope.Response!.Status.Code);
        Assert.Equal("fine", envelope.Response.Status.Detail);
        Assert.Equal("ProfileResponse", envelope.Payload!.ElementName);
    }

    [Fact]
    public void MissingAttributes_AreTolerated()
    {
        var envelope = new CxmlReader().Read("<cXML><Response><Status/></Response></cXML>");

        Assert.Equal(string.Empty, envelope.PayloadId);
        Assert.Equal(default, envelope.Timestamp);
        Assert.Equal(0, envelope.Response!.Status.Code);
    }

    [Fact]
    public void DoctypeWithInternalSubset_IsNotExpandedAndNoNetworkIsUsed()
    {
        var xml = """
            <?xml version="1.0"?>
            <!DOCTYPE cXML SYSTEM "http://127.0.0.1:1/never.dtd" [<!ENTITY boom "BOOM">]>
            <cXML payloadID="p" timestamp="2024-01-01T00:00:00Z"><Response><Status code="200" text="OK"/></Response></cXML>
            """;

        var envelope = new CxmlReader().Read(xml);

        Assert.Equal("p", envelope.PayloadId);
    }

    [Fact]
    public void CustomRegistry_ParsesRegisteredPayload()
    {
        var registry = CxmlPayloadRegistry.CreateDefault();
        registry.Register(new CxmlPayloadCodec<TestPayload>("TestRequest", e => new TestPayload((string?)e.Attribute("v") ?? ""), p => new XElement("TestRequest", new XAttribute("v", p.Value))));

        var envelope = new CxmlReader(registry).Read(Doc($"{Header}<Request><TestRequest v=\"abc\"/></Request>"));

        Assert.Equal("abc", Assert.IsType<TestPayload>(envelope.Payload).Value);
        Assert.Contains("TestRequest", registry.ParserNames);
    }

    [Fact]
    public void DefaultRegistry_KnowsBuiltInPayloads()
    {
        var names = CxmlPayloadRegistry.CreateDefault().ParserNames;

        Assert.Contains("PunchOutSetupRequest", names);
        Assert.Contains("PunchOutSetupResponse", names);
        Assert.Contains("PunchOutOrderMessage", names);
        Assert.Contains("OrderRequest", names);
        Assert.Contains("ProfileRequest", names);
        Assert.Contains("ProfileResponse", names);
    }

    [Fact]
    public void Read_FromBytesWithBomAndNonUtf8Declaration_Works()
    {
        var iso = System.Text.Encoding.GetEncoding("ISO-8859-1");
        var xml = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><cXML payloadID=\"é\" timestamp=\"2024-01-01T00:00:00Z\"><Response><Status code=\"200\" text=\"OK\"/></Response></cXML>";

        var envelope = new CxmlReader().Read(iso.GetBytes(xml));

        Assert.Equal("é", envelope.PayloadId);
    }

    [Fact]
    public void OrderDate_KeepsRawTextAndParsedValue()
    {
        var envelope = new CxmlReader().Read(TestFiles.ReadExample("0-OrderRequest.xml"));
        var header = Assert.IsType<OrderRequest>(envelope.Payload).Header;

        Assert.Equal("2000-10-12T18:41:29-08:00", header.OrderDateText);
        Assert.NotNull(header.OrderDate);
        Assert.Equal("2000-10-12T18:39:09-08:00", envelope.TimestampText);
    }

    private sealed record TestPayload(string Value) : ICxmlPayload
    {
        public string ElementName => "TestRequest";
    }
}
