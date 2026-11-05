using Cxml.Protocol.Documents.Profile;
using CxmlDevServer.Web.Tests.Support;

namespace CxmlDevServer.Web.Tests;

public sealed class PingAndProfileTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public PingAndProfileTests(WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_cxml_answers_a_dtd_valid_ping()
    {
        var response = await _factory.CreateClient().GetAsync("/cxml");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/xml", response.Content.Headers.ContentType?.ToString());
        var xml = await response.Content.ReadAsStringAsync();
        Assert.True(_factory.Validate(xml).IsValid, "The ping is not DTD valid.");
        Assert.Equal(200, _factory.Read(xml).Response?.Status.Code);
    }

    [Fact]
    public async Task Health_endpoint_is_up()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Rendered_profile_request_is_answered_with_a_profile_response()
    {
        var request = await _factory.RenderTemplateAsync("profile-request");

        var response = await _factory.CreateClient().PostCxmlAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var xml = await response.Content.ReadAsStringAsync();
        Assert.True(_factory.Validate(xml).IsValid);
        var envelope = _factory.Read(xml);
        Assert.Equal(200, envelope.Response?.Status.Code);
        var profile = Assert.IsType<ProfileResponse>(envelope.Response!.Payload);
        Assert.Contains(profile.Transactions, t => t.RequestName == "PunchOutSetupRequest" && t.Url.EndsWith("/cxml"));
    }

    [Fact]
    public async Task Malformed_xml_gets_cxml_406_in_an_http_200()
    {
        var response = await _factory.CreateClient().PostCxmlAsync("<cXML><Request>");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(406, _factory.Read(await response.Content.ReadAsStringAsync()).Response?.Status.Code);
    }

    [Fact]
    public async Task Wrong_shared_secret_gets_cxml_401()
    {
        var request = (await _factory.RenderTemplateAsync("profile-request")).Replace("devsecret", "wrong-secret");

        var response = await _factory.CreateClient().PostCxmlAsync(request);

        Assert.Equal(401, _factory.Read(await response.Content.ReadAsStringAsync()).Response?.Status.Code);
    }

    [Fact]
    public async Task A_document_posted_with_a_form_content_type_is_still_read_as_the_body()
    {
        var request = await _factory.RenderTemplateAsync("profile-request");

        var response = await _factory.CreateClient().PostAsync("/cxml",
            new StringContent(request, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));

        Assert.Equal(200, _factory.Read(await response.Content.ReadAsStringAsync()).Response?.Status.Code);
    }
}
