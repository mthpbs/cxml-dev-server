namespace CxmlDevServer.Web.Tests;

public class ScaffoldTests
{
    [Fact]
    public void WebCanSeeLowerLayers()
    {
        Assert.NotNull(typeof(CxmlDevServer.Web.Components.App));
        Assert.NotNull(typeof(CxmlDevServer.Infrastructure.InfrastructureAssembly));
        Assert.NotNull(typeof(CxmlDevServer.Core.CoreAssembly));
        Assert.NotNull(typeof(Cxml.Protocol.CxmlVersion));
    }
}
