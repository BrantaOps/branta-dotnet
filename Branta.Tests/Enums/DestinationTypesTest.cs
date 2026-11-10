using Branta;

namespace Branta.Tests.Enums;

public class DestinationTypesTest
{
    [Fact]
    public void PolygonEvm_IncludesUsdtAndUsdc()
    {
        Assert.Equal(
            [DestinationType.UsdtPolygon, DestinationType.UsdcPolygon],
            DestinationTypes.PolygonEvm);
    }

    [Theory]
    [InlineData(DestinationType.UsdtPolygon, true)]
    [InlineData(DestinationType.UsdcPolygon, true)]
    [InlineData(DestinationType.BitcoinAddress, false)]
    [InlineData(DestinationType.Bolt11, false)]
    [InlineData(DestinationType.ArkAddress, false)]
    [InlineData(DestinationType.SilentPayment, false)]
    public void IsPolygonEvm_MatchesFamily(DestinationType type, bool expected)
    {
        Assert.Equal(expected, DestinationTypes.IsPolygonEvm(type));
    }

    [Theory]
    [InlineData(DestinationType.BitcoinAddress, true)]
    [InlineData(DestinationType.UsdtPolygon, true)]
    [InlineData(DestinationType.UsdcPolygon, true)]
    [InlineData(DestinationType.Bolt11, false)]
    [InlineData(DestinationType.ArkAddress, false)]
    [InlineData(DestinationType.SilentPayment, false)]
    public void IsSecretKeyAddress_IncludesBitcoinAndPolygonFamily(DestinationType type, bool expected)
    {
        Assert.Equal(expected, DestinationTypes.IsSecretKeyAddress(type));
    }
}
