using System.Globalization;
using System.Security;
using Cxml.Protocol;

namespace CxmlDevServer.Core.Tests.Support;

/// <summary>Builders for small cXML test documents. Defaults address the seeded "Demo Buyer" partner.</summary>
public static class Docs
{
    public static string Envelope(
        string requestOrMessageXml,
        string senderDomain = "NetworkId",
        string senderIdentity = "AN01000000001",
        string? secret = "devsecret",
        string fromDomain = "NetworkId",
        string fromIdentity = "AN01000000001",
        string toDomain = "DUNS",
        string toIdentity = "123456789",
        string? payloadId = null,
        string? timestamp = null,
        string? doctype = null)
    {
        payloadId ??= PayloadIdGenerator.Generate();
        timestamp ??= CxmlTimestamp.Now();
        var secretXml = secret is null ? string.Empty : $"<SharedSecret>{SecurityElement.Escape(secret)}</SharedSecret>";
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            {doctype ?? "<!DOCTYPE cXML SYSTEM \"http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd\">"}
            <cXML payloadID="{payloadId}" timestamp="{timestamp}" xml:lang="en-US">
              <Header>
                <From><Credential domain="{fromDomain}"><Identity>{fromIdentity}</Identity></Credential></From>
                <To><Credential domain="{toDomain}"><Identity>{toIdentity}</Identity></Credential></To>
                <Sender>
                  <Credential domain="{senderDomain}"><Identity>{senderIdentity}</Identity>{secretXml}</Credential>
                  <UserAgent>Test Buyer 1.0</UserAgent>
                </Sender>
              </Header>
              {requestOrMessageXml}
            </cXML>
            """;
    }

    public static string Request(string payloadXml, string deploymentMode = "test") =>
        $"<Request deploymentMode=\"{deploymentMode}\">{payloadXml}</Request>";

    public static string OrderRequestPayload(
        string orderId = "PO-1",
        string type = "new",
        int? version = null,
        string total = "12.50",
        params (string Part, int Quantity, string Price)[] lines)
    {
        if (lines.Length == 0)
        {
            lines = [("P-1", 2, "5.00"), ("P-2", 1, "2.50")];
        }

        var versionAttr = version is { } v ? $" orderVersion=\"{v}\"" : string.Empty;
        var items = string.Concat(lines.Select((l, i) => $"""
            <ItemOut quantity="{l.Quantity}" lineNumber="{i + 1}">
              <ItemID><SupplierPartID>{l.Part}</SupplierPartID></ItemID>
              <ItemDetail>
                <UnitPrice><Money currency="USD">{l.Price}</Money></UnitPrice>
                <Description xml:lang="en">Item {l.Part}</Description>
                <UnitOfMeasure>EA</UnitOfMeasure>
                <Classification domain="UNSPSC">43211503</Classification>
              </ItemDetail>
            </ItemOut>
            """));
        return $"""
            <OrderRequest>
              <OrderRequestHeader orderID="{orderId}" orderDate="{CxmlTimestamp.Now()}" type="{type}"{versionAttr}>
                <Total><Money currency="USD">{total}</Money></Total>
                <BillTo>
                  <Address>
                    <Name xml:lang="en">Buyer Inc</Name>
                    <PostalAddress><Street>1 Main St</Street><City>Springfield</City><Country isoCountryCode="US">United States</Country></PostalAddress>
                  </Address>
                </BillTo>
              </OrderRequestHeader>
              {items}
            </OrderRequest>
            """;
    }

    public static string OrderRequest(
        string orderId = "PO-1",
        string type = "new",
        int? version = null,
        string total = "12.50",
        string? payloadId = null,
        params (string Part, int Quantity, string Price)[] lines) =>
        Envelope(Request(OrderRequestPayload(orderId, type, version, total, lines)), payloadId: payloadId);

    public static string PunchOutSetup(
        string operation = "create",
        string buyerCookie = "cookie-123",
        string? browserFormPost = "https://buyer.example/return",
        string itemOutXml = "",
        string? deploymentMode = "test",
        string? payloadId = null,
        string senderIdentity = "AN01000000001",
        string? secret = "devsecret")
    {
        var post = browserFormPost is null ? string.Empty : $"<BrowserFormPost><URL>{SecurityElement.Escape(browserFormPost)}</URL></BrowserFormPost>";
        var payload = $"""
            <PunchOutSetupRequest operation="{operation}">
              <BuyerCookie>{buyerCookie}</BuyerCookie>
              <Extrinsic name="UserEmail">jdoe@example.com</Extrinsic>
              {post}
              <Contact role="endUser"><Name xml:lang="en">John Doe</Name><Email>jdoe@example.com</Email></Contact>
              {itemOutXml}
            </PunchOutSetupRequest>
            """;
        return Envelope(Request(payload, deploymentMode ?? "production"), payloadId: payloadId, senderIdentity: senderIdentity, secret: secret);
    }

    public static string ItemOut(string partId, decimal quantity, string? aux = null, decimal? price = null, string name = "Ad hoc item", string unspsc = "43211503") =>
        $"""
        <ItemOut quantity="{quantity.ToString(CultureInfo.InvariantCulture)}">
          <ItemID>
            <SupplierPartID>{partId}</SupplierPartID>
            {(aux is null ? string.Empty : $"<SupplierPartAuxiliaryID>{aux}</SupplierPartAuxiliaryID>")}
          </ItemID>
          {(price is null ? string.Empty : $"""
          <ItemDetail>
            <UnitPrice><Money currency="USD">{price.Value.ToString(CultureInfo.InvariantCulture)}</Money></UnitPrice>
            <Description xml:lang="en">{name}</Description>
            <UnitOfMeasure>EA</UnitOfMeasure>
            <Classification domain="UNSPSC">{unspsc}</Classification>
          </ItemDetail>
          """)}
        </ItemOut>
        """;
}
