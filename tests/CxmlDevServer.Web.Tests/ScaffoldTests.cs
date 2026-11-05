namespace CxmlDevServer.Web.Tests;

public class ScaffoldTests
{
    [Fact]
    public void WebCanSeeLowerLayers()
    {
        Assert.NotNull(typeof(CxmlDevServer.Web.Components.App));
        Assert.NotNull(typeof(CxmlDevServer.Infrastructure.Persistence.CxmlDevDbContext));
        Assert.NotNull(typeof(CxmlDevServer.Core.Pipeline.CxmlInboundProcessor));
        Assert.NotNull(typeof(Cxml.Protocol.CxmlVersion));
    }
}
