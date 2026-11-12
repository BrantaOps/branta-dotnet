using Branta;

namespace Branta.Tests.Enums;

public class DestinationTypesTest
{
    [Fact]
    public void SecretKeyUriFamilies_MapSchemesToFamilies()
    {
        Assert.Equal(["pol", "eth", "bsc", "tron", "ton", "sol"], DestinationTypes.SecretKeyUriFamilies.Keys.ToArray());
        Assert.Equal([DestinationType.UsdtPolygon, DestinationType.UsdcPolygon], DestinationTypes.SecretKeyUriFamilies["pol"]);
        Assert.Equal([DestinationType.UsdtEthereum, DestinationType.UsdcEthereum], DestinationTypes.SecretKeyUriFamilies["eth"]);
        Assert.Equal([DestinationType.UsdtBsc, DestinationType.UsdcBsc], DestinationTypes.SecretKeyUriFamilies["bsc"]);
        Assert.Equal([DestinationType.UsdtTron], DestinationTypes.SecretKeyUriFamilies["tron"]);
        Assert.Equal([DestinationType.UsdtTon], DestinationTypes.SecretKeyUriFamilies["ton"]);
        Assert.Equal([DestinationType.UsdtSolana, DestinationType.UsdcSolana], DestinationTypes.SecretKeyUriFamilies["sol"]);
    }

    [Fact]
    public void IsSecretKeyAddress_CoversBitcoinAndEveryStableFamilyMember()
    {
        Assert.True(DestinationTypes.IsSecretKeyAddress(DestinationType.BitcoinAddress));
        foreach (var type in DestinationTypes.SecretKeyUriFamilies.Values.SelectMany(f => f))
            Assert.True(DestinationTypes.IsSecretKeyAddress(type), type.ToString());
        Assert.False(DestinationTypes.IsSecretKeyAddress(DestinationType.Bolt11));
        Assert.False(DestinationTypes.IsSecretKeyAddress(DestinationType.ArkAddress));
        Assert.False(DestinationTypes.IsSecretKeyAddress(DestinationType.SilentPayment));
    }

    [Fact]
    public void DestinationType_HasFourteenMembers_AndNoUsdcTon()
    {
        Assert.Equal(14, Enum.GetValues<DestinationType>().Length);
        Assert.Null(DestinationTypeConverter.TryParse("usdc_ton"));
    }

    [Theory]
    [InlineData(DestinationType.UsdtEthereum, "usdt_ethereum")]
    [InlineData(DestinationType.UsdcEthereum, "usdc_ethereum")]
    [InlineData(DestinationType.UsdtBsc, "usdt_bsc")]
    [InlineData(DestinationType.UsdcBsc, "usdc_bsc")]
    [InlineData(DestinationType.UsdtTron, "usdt_tron")]
    [InlineData(DestinationType.UsdtTon, "usdt_ton")]
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
