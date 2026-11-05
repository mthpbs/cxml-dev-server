using Cxml.Protocol.Envelope;
using Cxml.Protocol.Security;
using Cxml.Protocol.Tests.Support;

namespace Cxml.Protocol.Tests;

public class CredentialMacTests
{
    private const string ExpectedFromSpec = "cR6Jpz58nriXERDN";

    [Fact]
    public void GettingStartedGuideVector_ProducesPublishedValue()
    {
        var mac = CredentialMacCalculator.Compute(
            "NetworkId", "AN9900000100", "NetworkId", "AN9900000100",
            "2003-01-15T08:42:46-08:00", "2003-01-15T11:42:46-08:00",
            "abracadabra");

        Assert.Equal(ExpectedFromSpec, mac);
    }

    [Fact]
    public void Inputs_AreNormalised()
    {
        var mac = CredentialMacCalculator.Compute(
            "  NETWORKID", "  an9900000100\n", "networkid", " AN9900000100 ",
            "2003-01-15T08:42:46-08:00", "2003-01-15T11:42:46-08:00",
            "abracadabra");

        Assert.Equal(ExpectedFromSpec, mac);
    }

    [Fact]
    public void SharedSecret_IsNotNormalised()
    {
        var mac = CredentialMacCalculator.Compute(
            "NetworkId", "AN9900000100", "NetworkId", "AN9900000100",
            "2003-01-15T08:42:46-08:00", "2003-01-15T11:42:46-08:00",
            "Abracadabra");

        Assert.NotEqual(ExpectedFromSpec, mac);
    }

    [Fact]
    public void CaseSensitiveDomain_IsNotLowerCased()
    {
        Assert.Equal("AribaNetworkUserId", CredentialMacCalculator.NormalizeDomain("aribanetworkuserid"));
        Assert.Equal("duns", CredentialMacCalculator.NormalizeDomain("DUNS"));
    }

    [Fact]
    public void Mac_IsSixteenBase64Characters()
    {
        Assert.Equal(16, ExpectedFromSpec.Length);
        Assert.Equal(12, Convert.FromBase64String(ExpectedFromSpec).Length);
    }

    [Fact]
    public void Verify_UsesHeaderCredentialsAndMacDates()
    {
        var header = SampleDocuments.Header();
        header.From.Credentials[0] = new Credential { Domain = "NetworkId", Identity = "AN9900000100" };
        header.Sender.Credentials[0] = new Credential { Domain = "NetworkId", Identity = "AN9900000100" };
        var mac = new CredentialMac
        {
            CreationDate = "2003-01-15T08:42:46-08:00",
            ExpirationDate = "2003-01-15T11:42:46-08:00",
            Value = ExpectedFromSpec,
        };

        Assert.True(CredentialMacCalculator.Verify(header, mac, "abracadabra"));
        Assert.False(CredentialMacCalculator.Verify(header, mac, "wrong"));
        mac.Value = "AAAAAAAAAAAAAAAA";
        Assert.False(CredentialMacCalculator.Verify(header, mac, "abracadabra"));
    }

    [Fact]
    public void CredentialMac_WritesAndReadsBack()
    {
        var envelope = SampleDocuments.Request(new Cxml.Protocol.Documents.Profile.ProfileRequest());
        var sender = envelope.Header!.Sender.Credentials[0];
        sender.SharedSecret = null;
        sender.CredentialMac = new CredentialMac
        {
            CreationDate = "2016-01-15T08:42:46-08:00",
            ExpirationDate = "2016-01-15T11:42:46-08:00",
            Value = ExpectedFromSpec,
        };

        var xml = new Writing.CxmlWriter().Write(envelope);
        var parsed = new Parsing.CxmlReader().Read(xml).Header!.Sender.Credentials[0].CredentialMac!;

        Assert.True(new Validation.CxmlDtdValidator().Validate(xml).IsValid);
        Assert.Equal("FromSenderCredentials", parsed.Type);
        Assert.Equal("HMAC-SHA1-96", parsed.Algorithm);
        Assert.Equal(ExpectedFromSpec, parsed.Value);
    }
}
