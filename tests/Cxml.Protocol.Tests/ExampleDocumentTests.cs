using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Parsing;
using Cxml.Protocol.Tests.Support;
using Cxml.Protocol.Validation;

namespace Cxml.Protocol.Tests;

public class ExampleDocumentTests
{
    private readonly CxmlReader _reader = new();

    [Fact]
    public void OrderRequestExample_IsParsedIntoModel()
    {
        var envelope = _reader.Read(TestFiles.ReadExample("0-OrderRequest.xml"));

        Assert.Equal(CxmlEnvelopeKind.Request, envelope.Kind);
        Assert.Equal("32232995@ariba.acme.com", envelope.PayloadId);
        Assert.Equal("en-US", envelope.Lang);
        Assert.Equal(new DateTimeOffset(2000, 10, 12, 18, 39, 9, TimeSpan.FromHours(-8)), envelope.Timestamp);

        var header = envelope.Header!;
        Assert.Equal(2, header.From.Credentials.Count);
        Assert.True(header.From.Credentials[0].IsMarketplace);
        Assert.Equal("admin@acme.com", header.From.Primary!.Identity);
        Assert.Equal("DUNS", header.To.Credentials[0].Domain);
        Assert.Equal("942888711", header.To.Credentials[0].Identity);
        Assert.Equal("sysadmin@ariba.com", header.Sender.Primary!.Identity);
        Assert.Equal("abracadabra", header.Sender.Primary.SharedSecret);
        Assert.Equal("Ariba CSN 33", header.Sender.UserAgent);

        Assert.Equal("test", envelope.Request!.DeploymentMode);
        var order = Assert.IsType<OrderRequest>(envelope.Payload);
        Assert.Equal("DO1234", order.Header.OrderId);
        Assert.Equal(OrderRequestType.New, order.Header.Type);
        Assert.Equal(new DateTimeOffset(2000, 10, 12, 18, 41, 29, TimeSpan.FromHours(-8)), order.Header.OrderDate);
        Assert.Equal(187.60m, order.Header.Total.Amount);
        Assert.Equal("USD", order.Header.Total.Currency);
        Assert.Equal("Sunnyvale", order.Header.ShipTo!.Address.PostalAddress!.City);
        Assert.Equal(["Joe Smith", "Mailstop M-543"], order.Header.ShipTo.Address.PostalAddress.DeliverTo);
        Assert.Equal("United States", order.Header.ShipTo.Address.PostalAddress.Country);
        Assert.Equal("US", order.Header.ShipTo.Address.PostalAddress.IsoCountryCode);
        Assert.NotNull(order.Header.BillTo);
        Assert.Equal("1234567890123456", order.Header.Payment!.PCard!.Number);
        Assert.Equal("Anything well formed in XML.", order.Header.Comments.Single().Text);

        Assert.Equal(3, order.Items.Count);
        Assert.Equal([1, 4, 5], order.Items.Select(i => i.LineNumber!.Value));
        Assert.Equal([10m, 20m, 30m], order.Items.Select(i => i.Quantity));
        Assert.Equal("1233244", order.Items[0].ItemId.SupplierPartId);
        Assert.Equal(1.34m, order.Items[0].ItemDetail!.UnitPrice.Amount);
        Assert.Equal("hello from item 1", order.Items[0].ItemDetail!.Descriptions.Single().Text);
        Assert.Equal("SPSC", order.Items[0].ItemDetail!.Classifications.Single().Domain);
        Assert.Equal("2000-10-18", order.Items[0].RequestedDeliveryDate);
        Assert.NotNull(order.Items[0].Raw);
    }

    [Fact]
    public void OrderRequestExample_PassesDtdValidation()
    {
        var result = new CxmlDtdValidator().Validate(TestFiles.ReadExample("0-OrderRequest.xml"));

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues));
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ExampleDirectory_ContainsAllSpecExamples()
    {
        Assert.True(TestFiles.AllExamples().Count() >= 8);
    }

    [Theory]
    [MemberData(nameof(ExampleNames))]
    public void SpecExample_PassesDtdValidationAgainstItsDoctype(string fileName)
    {
        var result = new CxmlDtdValidator().Validate(TestFiles.ReadExample(fileName));

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues));
        Assert.DoesNotContain(result.Issues, i => i.Code == CxmlDtdValidator.VersionFallbackCode);
    }

    [Theory]
    [MemberData(nameof(ExampleNames))]
    public void SpecExample_CanBeReadWithoutError(string fileName)
    {
        var envelope = _reader.Read(TestFiles.ReadExample(fileName));

        Assert.NotEmpty(envelope.PayloadId);
        Assert.NotEqual(CxmlEnvelopeKind.None, envelope.Kind);
    }

    public static IEnumerable<object[]> ExampleNames() => TestFiles.ExampleNames();
}
