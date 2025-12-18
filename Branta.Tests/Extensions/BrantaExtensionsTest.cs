using Branta.Enums;
using Branta.Extensions;

namespace Branta.Tests.Extensions;

public class BrantaExtensionsTest
{
    [Fact]
    public void GetUrl()
    {
        Assert.Equal("http://localhost:3000", BrantaServerBaseUrl.Localhost.GetUrl());
    }
}
