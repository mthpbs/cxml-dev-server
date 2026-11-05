namespace CxmlDevServer.Core.Templates;

/// <summary>The raw cXML templates of the message sender. <c>{{name}}</c> placeholders are replaced (and XML-escaped) when rendered.</summary>
internal static class MessageTemplates
{
    private const string Header = """
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE cXML SYSTEM "http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd">
        <cXML payloadID="{{payloadId}}" timestamp="{{timestamp}}" xml:lang="en-US">
          <Header>
            <From>
              <Credential domain="{{fromDomain}}">
                <Identity>{{fromIdentity}}</Identity>
              </Credential>
            </From>
            <To>
              <Credential domain="{{toDomain}}">
                <Identity>{{toIdentity}}</Identity>
              </Credential>
            </To>
            <Sender>
              <Credential domain="{{senderDomain}}">
                <Identity>{{senderIdentity}}</Identity>
                <SharedSecret>{{sharedSecret}}</SharedSecret>
              </Credential>
              <UserAgent>{{userAgent}}</UserAgent>
            </Sender>
          </Header>
        """;

    private const string Footer = """
        </cXML>
        """;

    private const string SetupCommon = """
              <BuyerCookie>{{buyerCookie}}</BuyerCookie>
              <Extrinsic name="UserEmail">jdoe@example.com</Extrinsic>
              <Extrinsic name="UniqueName">jdoe</Extrinsic>
        """;

    private const string SetupContactAndShipTo = """
              <Contact role="endUser">
                <Name xml:lang="en">John Doe</Name>
                <Email>jdoe@example.com</Email>
              </Contact>
              <ShipTo>
                <Address>
                  <Name xml:lang="en">Acme Corporation</Name>
                  <PostalAddress name="default">
                    <Street>123 Anystreet</Street>
                    <City>Sunnyvale</City>
                    <State>CA</State>
                    <PostalCode>94089</PostalCode>
                    <Country isoCountryCode="US">United States</Country>
                  </PostalAddress>
                </Address>
              </ShipTo>
        """;

    private const string BrowserFormPost = """
              <BrowserFormPost>
                <URL>{{browserFormPostUrl}}</URL>
              </BrowserFormPost>
        """;

    private const string ItemOuts = """
              <ItemOut quantity="2">
                <ItemID>
                  <SupplierPartID>{{item1PartId}}</SupplierPartID>
                  <SupplierPartAuxiliaryID>{{item1Aux}}</SupplierPartAuxiliaryID>
                </ItemID>
                <ItemDetail>
                  <UnitPrice><Money currency="{{item1Currency}}">{{item1Price}}</Money></UnitPrice>
                  <Description xml:lang="en">{{item1Name}}</Description>
                  <UnitOfMeasure>{{item1Uom}}</UnitOfMeasure>
                  <Classification domain="UNSPSC">{{item1Unspsc}}</Classification>
                </ItemDetail>
              </ItemOut>
              <ItemOut quantity="1">
                <ItemID>
                  <SupplierPartID>{{item2PartId}}</SupplierPartID>
                  <SupplierPartAuxiliaryID>{{item2Aux}}</SupplierPartAuxiliaryID>
                </ItemID>
                <ItemDetail>
                  <UnitPrice><Money currency="{{item2Currency}}">{{item2Price}}</Money></UnitPrice>
                  <Description xml:lang="en">{{item2Name}}</Description>
                  <UnitOfMeasure>{{item2Uom}}</UnitOfMeasure>
                  <Classification domain="UNSPSC">{{item2Unspsc}}</Classification>
                </ItemDetail>
              </ItemOut>
        """;

    private static string Request(string body) =>
        string.Join('\n', Header, "  <Request deploymentMode=\"test\">", body, "  </Request>", Footer) + "\n";

    private static string Setup(string operation, params string[] parts) =>
        Request(string.Join('\n', new[] { $"    <PunchOutSetupRequest operation=\"{operation}\">" }.Concat(parts).Append("    </PunchOutSetupRequest>")));

    /// <summary>PunchOutSetupRequest, operation create.</summary>
    public static readonly string PunchOutSetupCreate = Setup("create", SetupCommon, BrowserFormPost, SetupContactAndShipTo);

    /// <summary>PunchOutSetupRequest, operation edit, with two items taken from the catalog.</summary>
    public static readonly string PunchOutSetupEdit = Setup("edit", SetupCommon, BrowserFormPost, SetupContactAndShipTo, ItemOuts);

    /// <summary>PunchOutSetupRequest, operation inspect, with two items taken from the catalog.</summary>
    public static readonly string PunchOutSetupInspect = Setup("inspect", SetupCommon, SetupContactAndShipTo, ItemOuts);

    /// <summary>An OrderRequest modelled on the specification's example.</summary>
    public static readonly string OrderRequest = Request("""
            <OrderRequest>
              <OrderRequestHeader orderID="{{orderId}}" orderDate="{{timestamp}}" type="new">
                <Total>
                  <Money currency="USD">187.60</Money>
                </Total>
                <ShipTo>
                  <Address>
                    <Name xml:lang="en">Acme</Name>
                    <PostalAddress name="default">
                      <DeliverTo>Joe Smith</DeliverTo>
                      <DeliverTo>Mailstop M-543</DeliverTo>
                      <Street>123 Anystreet</Street>
                      <City>Sunnyvale</City>
                      <State>CA</State>
                      <PostalCode>90489</PostalCode>
                      <Country isoCountryCode="US">United States</Country>
                    </PostalAddress>
                  </Address>
                </ShipTo>
                <BillTo>
                  <Address>
                    <Name xml:lang="en">Acme</Name>
                    <PostalAddress name="default">
                      <Street>123 Anystreet</Street>
                      <City>Sunnyvale</City>
                      <State>CA</State>
                      <PostalCode>90489</PostalCode>
                      <Country isoCountryCode="US">United States</Country>
                    </PostalAddress>
                  </Address>
                </BillTo>
                <Payment>
                  <PCard number="1234567890123456" expiration="2030-03-12"/>
                </Payment>
                <Comments xml:lang="en-US">Anything well formed in XML.</Comments>
              </OrderRequestHeader>
              <ItemOut quantity="10" requestedDeliveryDate="{{deliveryDate}}" lineNumber="1">
                <ItemID>
                  <SupplierPartID>1233244</SupplierPartID>
                </ItemID>
                <ItemDetail>
                  <UnitPrice><Money currency="USD">1.34</Money></UnitPrice>
                  <Description xml:lang="en">hello from item 1</Description>
                  <UnitOfMeasure>EA</UnitOfMeasure>
                  <Classification domain="UNSPSC">12345</Classification>
                  <ManufacturerPartID>234</ManufacturerPartID>
                  <ManufacturerName>foobar</ManufacturerName>
                </ItemDetail>
              </ItemOut>
              <ItemOut quantity="20" requestedDeliveryDate="{{deliveryDate}}" lineNumber="2">
                <ItemID>
                  <SupplierPartID>1233245</SupplierPartID>
                </ItemID>
                <ItemDetail>
                  <UnitPrice><Money currency="USD">2.68</Money></UnitPrice>
                  <Description xml:lang="en">hello from item 2</Description>
                  <UnitOfMeasure>EA</UnitOfMeasure>
                  <Classification domain="UNSPSC">12346</Classification>
                  <ManufacturerPartID>235</ManufacturerPartID>
                  <ManufacturerName>foobar</ManufacturerName>
                </ItemDetail>
              </ItemOut>
              <ItemOut quantity="30" requestedDeliveryDate="{{deliveryDate}}" lineNumber="3">
                <ItemID>
                  <SupplierPartID>1233246</SupplierPartID>
                </ItemID>
                <ItemDetail>
                  <UnitPrice><Money currency="USD">4.02</Money></UnitPrice>
                  <Description xml:lang="en">hello from item 3</Description>
                  <UnitOfMeasure>EA</UnitOfMeasure>
                  <Classification domain="UNSPSC">12347</Classification>
                  <ManufacturerPartID>236</ManufacturerPartID>
                  <ManufacturerName>foobar</ManufacturerName>
                </ItemDetail>
              </ItemOut>
            </OrderRequest>
        """);

    /// <summary>A ProfileRequest.</summary>
    public static readonly string ProfileRequest = Request("    <ProfileRequest/>");
}
