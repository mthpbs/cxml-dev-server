using Cxml.Protocol.Common;
using Cxml.Protocol.Documents;
using Cxml.Protocol.Documents.Orders;
using Cxml.Protocol.Documents.Profile;
using Cxml.Protocol.Documents.PunchOut;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Parsing;
using Cxml.Protocol.Tests.Support;
using Cxml.Protocol.Validation;
using Cxml.Protocol.Writing;

namespace Cxml.Protocol.Tests;

/// <summary>Writes each payload, validates the output against the DTD and reads it back.</summary>
public class RoundTripTests
{
    private readonly CxmlWriter _writer = new();
    private readonly CxmlReader _reader = new();
    private readonly CxmlDtdValidator _validator = new();

    private string WriteAndValidate(CxmlEnvelope envelope)
    {
        var xml = _writer.Write(envelope);
        var result = _validator.Validate(xml);
        Assert.True(result.IsValid, xml + Environment.NewLine + string.Join(Environment.NewLine, result.Issues));
        Assert.Empty(result.Issues);
        return xml;
    }

    [Fact]
    public void Output_StartsWithDeclarationAndDoctype_WithoutBomOrWhitespace()
    {
        var xml = WriteAndValidate(SampleDocuments.Request(new ProfileRequest()));

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xml, StringComparison.Ordinal);
        Assert.Contains("<!DOCTYPE cXML SYSTEM \"http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd\">", xml);
        Assert.NotEqual('﻿', xml[0]);
        Assert.False(char.IsWhiteSpace(xml[0]));

        var bytes = _writer.WriteBytes(SampleDocuments.Request(new ProfileRequest()));
        Assert.Equal((byte)'<', bytes[0]);
    }

    [Theory]
    [InlineData(PunchOutOperation.Create)]
    [InlineData(PunchOutOperation.Edit)]
    [InlineData(PunchOutOperation.Inspect)]
    [InlineData(PunchOutOperation.Source)]
    public void PunchOutSetupRequest_RoundTrips(PunchOutOperation operation)
    {
        var source = SampleDocuments.SetupRequest(operation);
        var xml = WriteAndValidate(SampleDocuments.Request(source));

        var envelope = _reader.Read(xml);
        var parsed = Assert.IsType<PunchOutSetupRequest>(envelope.Payload);

        Assert.Equal(operation, parsed.Operation);
        Assert.Equal("cookie-1234", parsed.BuyerCookie);
        Assert.Equal("https://buyer.example.com/punchout/return", parsed.BrowserFormPostUrl);
        Assert.Equal(["UserEmail", "CostCenter"], parsed.Extrinsics.Select(e => e.Name));
        Assert.Equal("CC-7", parsed.Extrinsics[1].Value);
        Assert.Equal("Joe Smith", parsed.Contacts.Single().Name);
        Assert.Equal("endUser", parsed.Contacts.Single().Role);
        Assert.Equal("https://supplier.example.com/setup", parsed.SupplierSetupUrl);
        Assert.Equal("Acme Ünïcode", parsed.ShipTo!.Address.Name);
        Assert.Equal(source.Items.Count, parsed.Items.Count);
        Assert.Equal(source.SelectedItem?.SupplierPartId, parsed.SelectedItem?.SupplierPartId);
        if (operation == PunchOutOperation.Edit)
        {
            var item = parsed.Items.Single();
            Assert.Equal(2m, item.Quantity);
            Assert.Equal("cookie-42", item.ItemId.SupplierPartAuxiliaryId);
            Assert.Equal(12.50m, item.ItemDetail!.UnitPrice.Amount);
        }

        // Envelope metadata survives.
        Assert.Equal("1234.5678.1@test", envelope.PayloadId);
        Assert.Equal(SampleDocuments.Now, envelope.Timestamp);
        Assert.Equal("test", envelope.Request!.DeploymentMode);
        Assert.Equal("s3cret", envelope.Header!.Sender.Primary!.SharedSecret);
        Assert.Equal("Test Procurement 1.0", envelope.Header.Sender.UserAgent);
    }

    [Fact]
    public void PunchOutSetupRequest_PreservesXmlInsideBuyerCookie()
    {
        var request = SampleDocuments.SetupRequest(PunchOutOperation.Create);
        request.BuyerCookie = "<Session id=\"7\"><User>Zoë &amp; Co</User></Session>";

        // BuyerCookie is declared ANY, but the DTD only accepts declared elements, so a
        // cookie with markup is intentionally not DTD-validated here.
        var xml = _writer.Write(SampleDocuments.Request(request));
        var parsed = Assert.IsType<PunchOutSetupRequest>(_reader.Read(xml).Payload);

        Assert.Equal(request.BuyerCookie, parsed.BuyerCookie);
    }

    [Fact]
    public void PunchOutSetupResponse_RoundTrips()
    {
        var response = CxmlResponses.Ok(new PunchOutSetupResponse { StartPageUrl = "https://supplier.example.com/punchout/abc?x=1&y=2" });

        var xml = WriteAndValidate(response);
        var parsed = _reader.Read(xml);

        Assert.Equal(CxmlEnvelopeKind.Response, parsed.Kind);
        Assert.Equal(200, parsed.Response!.Status.Code);
        Assert.Equal("OK", parsed.Response.Status.Text);
        Assert.Equal("https://supplier.example.com/punchout/abc?x=1&y=2",
            Assert.IsType<PunchOutSetupResponse>(parsed.Payload).StartPageUrl);
    }

    [Fact]
    public void PunchOutOrderMessage_RoundTrips_WithNonAsciiAndBuyerCookie()
    {
        var message = SampleDocuments.OrderMessage();
        var envelope = new CxmlEnvelope
        {
            PayloadId = "msg-1@supplier",
            Timestamp = SampleDocuments.Now,
            Lang = "en-US",
            Header = SampleDocuments.Header(senderSecret: null!),
            Message = new CxmlMessage { DeploymentMode = "test", InReplyTo = "1234.5678.1@test", Payload = message },
        };
        envelope.Header.Sender.Credentials[0].SharedSecret = null;

        var xml = WriteAndValidate(envelope);
        var parsed = _reader.Read(xml);

        Assert.Equal(CxmlEnvelopeKind.Message, parsed.Kind);
        Assert.Equal("1234.5678.1@test", parsed.Message!.InReplyTo);
        var order = Assert.IsType<PunchOutOrderMessage>(parsed.Payload);
        Assert.Equal(message.BuyerCookie, order.BuyerCookie);
        Assert.Equal(PunchOutOperation.Edit, order.Header.OperationAllowed);
        Assert.Equal(25.00m, order.Header.Total.Amount);
        Assert.Equal("USD", order.Header.Total.Currency);
        Assert.Equal(5.00m, order.Header.Shipping!.Cost.Amount);
        Assert.Equal(2.10m, order.Header.Tax!.Amount.Amount);
        Assert.Equal("sales", order.Header.Tax.Details.Single().Category);

        var item = order.Items.Single();
        Assert.Equal(1, item.LineNumber);
        Assert.Equal("Wireless mouse – ergonomic 日本", item.ItemDetail.Descriptions.Single().Text);
        Assert.Equal("Mouse", item.ItemDetail.Descriptions.Single().ShortName);
        Assert.Equal("Logi & Co <test>", item.ItemDetail.ManufacturerName);
        Assert.Equal("UNSPSC", item.ItemDetail.Classifications.Single().Domain);
        Assert.Equal("43211708", item.ItemDetail.Classifications.Single().Value);
        Assert.Equal(3, item.ItemDetail.LeadTime);
        Assert.Equal("cookie-42", item.ItemId.SupplierPartAuxiliaryId);
        Assert.Equal("B-9", item.ItemId.BuyerPartId);
        Assert.Equal("Marketing", item.Distributions.Single().Segments.Single().Description);
        Assert.Equal("Gift wrap please", item.Comments!.Text);
        Assert.Equal("black", item.ItemDetail.Extrinsics.Single().Value);
        Assert.Equal("x", item.Extrinsics.Single().Value);
    }

    [Fact]
    public void PunchOutOrderMessage_WithStatus204AndNoItems_IsValid()
    {
        var envelope = new CxmlEnvelope
        {
            PayloadId = "msg-2@supplier",
            Timestamp = SampleDocuments.Now,
            Header = SampleDocuments.Header(),
            Message = new CxmlMessage
            {
                InReplyTo = "x",
                Status = new Status { Code = 204, Text = "No Content" },
                Payload = new PunchOutOrderMessage
                {
                    BuyerCookie = "c",
                    Header = new PunchOutOrderMessageHeader { OperationAllowed = PunchOutOperation.Create, Total = new Money(0m, "USD") },
                },
            },
        };
        envelope.Header.Sender.Credentials[0].SharedSecret = null;

        var parsed = _reader.Read(WriteAndValidate(envelope));

        Assert.Equal(204, parsed.Message!.Status!.Code);
        Assert.Empty(Assert.IsType<PunchOutOrderMessage>(parsed.Payload).Items);
    }

    [Fact]
    public void OrderRequest_RoundTrips()
    {
        var source = SampleDocuments.Order();
        var xml = WriteAndValidate(SampleDocuments.Request(source));

        var parsed = Assert.IsType<OrderRequest>(_reader.Read(xml).Payload);
        var header = parsed.Header;

        Assert.Equal("PO-1001", header.OrderId);
        Assert.Equal(OrderRequestType.New, header.Type);
        Assert.Equal(1, header.OrderVersion);
        Assert.Equal("regular", header.OrderType);
        Assert.Equal(SampleDocuments.Now, header.OrderDate);
        Assert.Equal(25.00m, header.Total.Amount);
        Assert.Equal("Acme Billing", header.BillTo!.Address.Name);
        Assert.Equal("408", header.ShipTo!.Address.Phone!.AreaOrCityCode);
        Assert.Equal("1234567890123456", header.Payment!.PCard!.Number);
        Assert.Equal("Rush – please", header.Comments.Single().Text);
        Assert.Equal("9999.1.2@buyer", header.DocumentReferencePayloadId);
        Assert.Equal("R-1", header.Extrinsics.Single().Value);
        var line = parsed.Items.Single();
        Assert.Equal(2.5m, line.Quantity);
        Assert.Equal("2024-05-10", line.RequestedDeliveryDate);
        Assert.Equal("line note", line.Comments.Single().Text);
    }

    [Theory]
    [InlineData(OrderRequestType.Update)]
    [InlineData(OrderRequestType.Delete)]
    public void OrderRequest_UpdateAndDelete_RoundTrip(OrderRequestType type)
    {
        var order = SampleDocuments.Order();
        order.Header.Type = type;

        var parsed = Assert.IsType<OrderRequest>(_reader.Read(WriteAndValidate(SampleDocuments.Request(order))).Payload);

        Assert.Equal(type, parsed.Header.Type);
    }

    [Fact]
    public void ProfileRequest_RoundTrips()
    {
        var parsed = _reader.Read(WriteAndValidate(SampleDocuments.Request(new ProfileRequest())));

        Assert.IsType<ProfileRequest>(parsed.Payload);
    }

    [Fact]
    public void ProfileResponse_RoundTrips()
    {
        var xml = WriteAndValidate(CxmlResponses.Ok(SampleDocuments.Profile()));

        var profile = Assert.IsType<ProfileResponse>(_reader.Read(xml).Payload);

        Assert.Equal(SampleDocuments.Now, profile.EffectiveDate);
        Assert.Equal(SampleDocuments.Now.AddHours(-1), profile.LastRefresh);
        Assert.Equal("Server.Version", profile.Options.Single().Name);
        Assert.Equal(2, profile.Transactions.Count);
        Assert.Equal("PunchOutSetupRequest", profile.Transactions[1].RequestName);
        Assert.Equal("https://s.example.com/cxml", profile.Transactions[1].Url);
        Assert.Equal("v", profile.Transactions[1].Options.Single().Value);
    }

    [Theory]
    [InlineData(406, "Not Acceptable", "Element 'X' is not allowed")]
    [InlineData(401, null, null)]
    [InlineData(500, null, "Boom <&>")]
    [InlineData(204, null, null)]
    public void StatusResponse_RoundTrips(int code, string? text, string? detail)
    {
        var xml = WriteAndValidate(CxmlResponses.Status(code, text, detail));

        var parsed = _reader.Read(xml);

        Assert.Equal(code, parsed.Response!.Status.Code);
        Assert.Equal(text ?? CxmlStatusCodes.DefaultText(code), parsed.Response.Status.Text);
        Assert.Equal(detail, parsed.Response.Status.Detail);
        Assert.Null(parsed.Payload);
    }

    [Fact]
    public void Indent_ProducesValidIndentedXml()
    {
        var writer = new CxmlWriter(CxmlPayloadRegistry.CreateDefault(), new CxmlWriterOptions { Indent = true });

        var xml = writer.Write(SampleDocuments.Request(SampleDocuments.Order()));

        Assert.Contains("\n  <Header>", xml);
        Assert.True(_validator.Validate(xml).IsValid);
    }

    [Fact]
    public void WriteBytes_IsUtf8WithoutBom_AndDecodesToSameText()
    {
        var envelope = SampleDocuments.Request(SampleDocuments.Order());

        var bytes = _writer.WriteBytes(envelope);

        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal(_writer.Write(envelope), System.Text.Encoding.UTF8.GetString(bytes));
        Assert.Equal(envelope.PayloadId, _reader.Read(bytes).PayloadId);
    }

    [Fact]
    public void Writer_WithoutPayloadWriter_Throws()
    {
        var writer = new CxmlWriter(new CxmlPayloadRegistry());

        Assert.Throws<InvalidOperationException>(() => writer.Write(SampleDocuments.Request(new ProfileRequest())));
    }

    [Fact]
    public void Writer_RequiresHeaderForRequests()
    {
        var envelope = SampleDocuments.Request(new ProfileRequest());
        envelope.Header = null;

        Assert.Throws<InvalidOperationException>(() => _writer.Write(envelope));
    }

    [Fact]
    public void UnknownPayload_IsWrittenBackVerbatim()
    {
        var xml = _writer.Write(SampleDocuments.Request(new UnknownPayload(System.Xml.Linq.XElement.Parse("<FooRequest a=\"1\"><Bar>x</Bar></FooRequest>"))));

        Assert.Contains("<FooRequest a=\"1\"><Bar>x</Bar></FooRequest>", xml);
    }
}
