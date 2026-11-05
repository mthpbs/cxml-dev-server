using Cxml.Protocol.Envelope;
using Cxml.Protocol.Documents.PunchOut;

namespace Cxml.Protocol.Tests;

public class StatusCodeTests
{
    public static TheoryData<int, string> DefaultTexts => new()
    {
        { 200, "OK" }, { 201, "Accepted" }, { 204, "No Content" }, { 400, "Bad Request" },
        { 401, "Unauthorized" }, { 402, "Payment Required" }, { 403, "Forbidden" }, { 406, "Not Acceptable" },
        { 409, "Conflict" }, { 412, "Precondition Failed" }, { 417, "Expectation Failed" }, { 450, "Not Implemented" },
        { 475, "Signature Required" }, { 476, "Signature Verification Failed" }, { 477, "Signature Unacceptable" },
        { 500, "Internal Server Error" }, { 550, "Unable to reach cXML server" }, { 551, "Unable to forward request" },
        { 560, "Temporary server error" },
    };

    [Theory]
    [MemberData(nameof(DefaultTexts))]
    public void DefaultText_MatchesSpec(int code, string text)
    {
        Assert.Equal(text, CxmlStatusCodes.DefaultText(code));
        Assert.Contains(code, CxmlStatusCodes.KnownCodes);
    }

    [Theory]
    [InlineData(200, true, false, false)]
    [InlineData(201, true, false, false)]
    [InlineData(204, true, false, false)]
    [InlineData(299, true, false, false)]
    [InlineData(400, false, true, false)]
    [InlineData(406, false, true, false)]
    [InlineData(450, false, true, false)]
    [InlineData(499, false, true, false)]
    [InlineData(500, false, false, true)]
    [InlineData(560, false, false, true)]
    [InlineData(199, false, false, false)]
    [InlineData(300, false, false, false)]
    public void Classification_Helpers(int code, bool success, bool permanent, bool transient)
    {
        Assert.Equal(success, CxmlStatusCodes.IsSuccess(code));
        Assert.Equal(permanent, CxmlStatusCodes.IsPermanent(code));
        Assert.Equal(transient, CxmlStatusCodes.IsTransient(code));
    }

    [Fact]
    public void ConstantsHaveSpecValues()
    {
        Assert.Equal(406, CxmlStatusCodes.NotAcceptable);
        Assert.Equal(412, CxmlStatusCodes.PreconditionFailed);
        Assert.Equal(450, CxmlStatusCodes.NotImplemented);
        Assert.Equal(560, CxmlStatusCodes.TemporaryServerError);
    }

    [Fact]
    public void ResponseHelper_FillsDefaultTextAndOmitsPayloadForErrors()
    {
        var payload = new PunchOutSetupResponse { StartPageUrl = "https://x" };

        var error = CxmlResponses.Status(500, detail: "boom", payload: payload);
        var success = CxmlResponses.Status(200, payload: payload);

        Assert.Equal("Internal Server Error", error.Response!.Status.Text);
        Assert.Equal("boom", error.Response.Status.Detail);
        Assert.Null(error.Response.Payload);
        Assert.Same(payload, success.Response!.Payload);
        Assert.False(string.IsNullOrEmpty(error.PayloadId));
    }

    [Fact]
    public void ResponseHelper_IgnoresBlankDetail()
    {
        Assert.Null(CxmlResponses.Status(200, detail: "  ").Response!.Status.Detail);
    }
}
