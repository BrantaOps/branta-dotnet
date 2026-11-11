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

    [Fact]
    public void SecretKeyUriFamilies_MapSchemesToFamilies()
    {
        Assert.Equal(["pol", "eth", "bsc", "tron", "ton", "sol"], DestinationTypes.SecretKeyUriFamilies.Keys.ToArray());
        Assert.Equal([DestinationType.UsdtEthereum, DestinationType.UsdcEthereum], DestinationTypes.SecretKeyUriFamilies["eth"]);
        Assert.Equal([DestinationType.UsdtBsc, DestinationType.UsdcBsc], DestinationTypes.SecretKeyUriFamilies["bsc"]);
        Assert.Equal([DestinationType.UsdtTron], DestinationTypes.SecretKeyUriFamilies["tron"]);
        Assert.Equal([DestinationType.UsdtTon, DestinationType.UsdcTon], DestinationTypes.SecretKeyUriFamilies["ton"]);
        Assert.Equal([DestinationType.UsdtSolana, DestinationType.UsdcSolana], DestinationTypes.SecretKeyUriFamilies["sol"]);
    }

    [Fact]
    public void IsSecretKeyAddress_CoversBitcoinAndEveryStableFamilyMember()
    {
        Assert.True(DestinationTypes.IsSecretKeyAddress(DestinationType.BitcoinAddress));
        foreach (var type in DestinationTypes.SecretKeyUriFamilies.Values.SelectMany(f => f))
            Assert.True(DestinationTypes.IsSecretKeyAddress(type), type.ToString());
    }

    [Theory]
    [InlineData(DestinationType.UsdtEthereum, "usdt_ethereum")]
    [InlineData(DestinationType.UsdcEthereum, "usdc_ethereum")]
    [InlineData(DestinationType.UsdtBsc, "usdt_bsc")]
    [InlineData(DestinationType.UsdcBsc, "usdc_bsc")]
    [InlineData(DestinationType.UsdtTron, "usdt_tron")]
    [InlineData(DestinationType.UsdtTon, "usdt_ton")]
    [InlineData(DestinationType.UsdcTon, "usdc_ton")]
    [InlineData(DestinationType.UsdtSolana, "usdt_solana")]
    [InlineData(DestinationType.UsdcSolana, "usdc_solana")]
    public void StableTypes_SerializeToWireNames(DestinationType type, string wire)
    {
        Assert.Equal($"\"{wire}\"", System.Text.Json.JsonSerializer.Serialize(type));
        Assert.Equal(type, System.Text.Json.JsonSerializer.Deserialize<DestinationType>($"\"{wire}\""));
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
