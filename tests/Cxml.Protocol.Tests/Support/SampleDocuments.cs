using Cxml.Protocol.Common;
using Cxml.Protocol.Documents;
using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Documents.Profile;
using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Envelope;

namespace Cxml.Protocol.Tests.Support;

/// <summary>Builders for envelopes used by the round-trip tests.</summary>
internal static class SampleDocuments
{
    public static readonly DateTimeOffset Now = new(2024, 5, 1, 10, 30, 15, TimeSpan.FromHours(2));

    public static CxmlHeader Header(string senderSecret = "s3cret") => new()
    {
        From = new Party { Credentials = [new Credential { Domain = "NetworkId", Identity = "BUYER-1" }] },
        To = new Party { Credentials = [new Credential { Domain = "DUNS", Identity = "123456789" }] },
        Sender = new SenderParty
        {
            Credentials = [new Credential { Domain = "NetworkId", Identity = "BUYER-1", SharedSecret = senderSecret }],
            UserAgent = "Test Procurement 1.0",
        },
    };

    public static CxmlEnvelope Request(ICxmlPayload payload) => new()
    {
        PayloadId = "1234.5678.1@test",
        Timestamp = Now,
        Lang = "en-US",
        Header = Header(),
        Request = new CxmlRequest { DeploymentMode = "test", Payload = payload },
    };

    public static Address Address(string name = "Acme Ünïcode") => new()
    {
        Name = name,
        NameLang = "en",
        PostalAddress = new PostalAddress
        {
            Name = "default",
            DeliverTo = ["Joe Smith", "Mailstop M-543"],
            Street = ["123 Anystreet"],
            City = "Sunnyvale",
            State = "CA",
            PostalCode = "90489",
            Country = "United States",
            IsoCountryCode = "US",
        },
        Email = "joe@example.com",
        Phone = new Phone { CountryCode = "1", IsoCountryCode = "US", AreaOrCityCode = "408", Number = "5551234" },
    };

    public static ItemId ItemId(string id = "P-100", string? aux = "cookie-42") => new()
    {
        SupplierPartId = id,
        SupplierPartAuxiliaryId = aux,
        BuyerPartId = "B-9",
    };

    public static ItemDetail ItemDetail() => new()
    {
        UnitPrice = new Money(12.50m, "USD"),
        Descriptions = [new Description("en", "Wireless mouse – ergonomic 日本", "Mouse")],
        UnitOfMeasure = "EA",
        Classifications = [new Classification("UNSPSC", "43211708")],
        ManufacturerPartId = "MX-1",
        ManufacturerName = "Logi & Co <test>",
        Url = "https://example.com/p/100",
        LeadTime = 3,
        Extrinsics = [new Extrinsic("color", "black")],
    };

    public static PunchOutSetupRequest SetupRequest(PunchOutOperation operation)
    {
        var request = new PunchOutSetupRequest
        {
            Operation = operation,
            BuyerCookie = "cookie-1234",
            BrowserFormPostUrl = "https://buyer.example.com/punchout/return",
            Extrinsics = [new Extrinsic("UserEmail", "joe@example.com"), new Extrinsic("CostCenter", "CC-7")],
            Contacts = [new Contact { Role = "endUser", Name = "Joe Smith", Emails = ["joe@example.com"] }],
            SupplierSetupUrl = "https://supplier.example.com/setup",
            ShipTo = new ShipTo { Address = Address() },
        };
        if (operation is PunchOutOperation.Edit or PunchOutOperation.Inspect)
        {
            request.Items = [new ItemOut
            {
                Quantity = 2,
                ItemId = ItemId(),
                ItemDetail = ItemDetail(),
            }];
        }

        if (operation == PunchOutOperation.Source)
        {
            request.SelectedItem = ItemId("P-SEL", null);
        }

        return request;
    }

    public static PunchOutOrderMessage OrderMessage() => new()
    {
        BuyerCookie = "abc &amp; déf 日本",
        Header = new PunchOutOrderMessageHeader
        {
            OperationAllowed = PunchOutOperation.Edit,
            Total = new Money(25.00m, "USD"),
            ShipTo = new ShipTo { Address = Address() },
            Shipping = new Shipping { Cost = new Money(5.00m, "USD"), Description = new Description("en", "Ground") },
            Tax = new Tax
            {
                Amount = new Money(2.10m, "USD"),
                Description = new Description("en", "Sales tax"),
                Details = [new TaxDetail { Category = "sales", PercentageRate = "8.25", TaxAmount = new Money(2.10m, "USD") }],
            },
        },
        Items =
        [
            new ItemIn
            {
                Quantity = 2,
                LineNumber = 1,
                ItemId = ItemId(),
                ItemDetail = ItemDetail(),
                Distributions =
                [
                    new Distribution
                    {
                        AccountingName = "Account",
                        Segments = [new AccountingSegment { Id = "100", Name = "CostCenter", Description = "Marketing" }],
                        Charge = new Money(25.00m, "USD"),
                    },
                ],
                Comments = new Comments { Text = "Gift wrap please", Lang = "en" },
                Extrinsics = [new Extrinsic("note", "x")],
            },
        ],
    };

    public static OrderRequest Order() => new()
    {
        Header = new OrderRequestHeader
        {
            OrderId = "PO-1001",
            OrderDate = Now,
            Type = OrderRequestType.New,
            OrderVersion = 1,
            OrderType = "regular",
            Total = new Money(25.00m, "USD"),
            ShipTo = new ShipTo { Address = Address() },
            BillTo = new BillTo { Address = Address("Acme Billing") },
            Shipping = new Shipping { Cost = new Money(5.00m, "USD"), Description = new Description("en", "Ground") },
            Payment = new Payment { PCard = new PCard { Number = "1234567890123456", Expiration = "2030-01-31", Name = "J Smith" } },
            Contacts = [new Contact { Role = "buyer", Name = "Joe", NameLang = "en" }],
            Comments = [new Comments { Text = "Rush – please", Lang = "en-US" }],
            DocumentReferencePayloadId = "9999.1.2@buyer",
            Extrinsics = [new Extrinsic("Ref", "R-1")],
        },
        Items =
        [
            new ItemOut
            {
                Quantity = 2.5m,
                LineNumber = 1,
                RequestedDeliveryDate = "2024-05-10",
                ItemId = ItemId(),
                ItemDetail = ItemDetail(),
                Comments = [new Comments { Text = "line note", Lang = "en" }],
            },
        ],
    };

    public static ProfileResponse Profile() => new()
    {
        EffectiveDate = Now,
        LastRefresh = Now.AddHours(-1),
        Options = [new ProfileOption { Name = "Server.Version", Value = "1" }],
        Transactions =
        [
            new ProfileTransaction { RequestName = "ProfileRequest", Url = "https://s.example.com/cxml" },
            new ProfileTransaction
            {
                RequestName = "PunchOutSetupRequest",
                Url = "https://s.example.com/cxml",
                Options = [new ProfileOption { Name = "opt", Value = "v" }],
            },
        ],
    };
}
