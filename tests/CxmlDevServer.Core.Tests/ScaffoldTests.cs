namespace CxmlDevServer.Core.Tests;

public class ScaffoldTests
{
    [Fact]
    public void CoreCanUseProtocolLibrary()
    {
        Assert.NotNull(typeof(CoreAssembly));
        Assert.Equal("1.2.071", Cxml.Protocol.CxmlVersion.Current);
    }
}
