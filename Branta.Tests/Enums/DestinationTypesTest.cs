using Branta;

namespace Branta.Tests.Enums;

public class DestinationTypesTest
{
    [Fact]
    public void EvmTokenRails_CoverFiveNetworksWithOneTypeEach()
    {
        Assert.Equal(10, DestinationTypes.EvmTokenRails.Count);
        Assert.Equal(
            new long[] { 1, 56, 137, 8453, 42161 },
            DestinationTypes.EvmTokenRails.Select(r => r.ChainId).Distinct().OrderBy(c => c).ToArray());
        Assert.Equal(10, DestinationTypes.EvmTokenRails.Select(r => r.Type).Distinct().Count());
        Assert.All(DestinationTypes.EvmTokenRails, rail => Assert.Equal(rail.Contract.ToLowerInvariant(), rail.Contract));
    }

    [Fact]
    public void EvmTokenType_SelectsByChainIdAndContract()
    {
        const string baseUsdc = "0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913";
        Assert.Equal(DestinationType.UsdcBase, DestinationTypes.EvmTokenType(8453, baseUsdc));
        Assert.Equal(DestinationType.UsdcBase, DestinationTypes.EvmTokenType(8453, baseUsdc.ToLowerInvariant()));
        // Same contract on the wrong chain is not a rail.
        Assert.Null(DestinationTypes.EvmTokenType(1, baseUsdc));
        // Polygon bridged USDC.e is not the native USDC contract.
        Assert.Null(DestinationTypes.EvmTokenType(137, "0x2791bca1f2de4661ed88a30c99a7a9449aa84174"));
        // Arbitrum Nova is not Arbitrum One.
        Assert.Null(DestinationTypes.EvmTokenType(42170, "0xaf88d065e77c8cc2239327c5edb3a432268e5831"));
    }

    [Fact]
    public void IsSecretKeyAddress_CoversBitcoinEvmRailsAndPostOnlyTypes()
    {
        Assert.True(DestinationTypes.IsSecretKeyAddress(DestinationType.BitcoinAddress));
        foreach (var rail in DestinationTypes.EvmTokenRails)
            Assert.True(DestinationTypes.IsSecretKeyAddress(rail.Type), rail.Type.ToString());
        // Tron, TON, and Solana still encrypt on AddPaymentAsync. They have no verify URI.
        Assert.True(DestinationTypes.IsSecretKeyAddress(DestinationType.UsdtTron));
        Assert.True(DestinationTypes.IsSecretKeyAddress(DestinationType.UsdtTon));
        Assert.True(DestinationTypes.IsSecretKeyAddress(DestinationType.UsdtSolana));
        Assert.True(DestinationTypes.IsSecretKeyAddress(DestinationType.UsdcSolana));
        Assert.False(DestinationTypes.IsSecretKeyAddress(DestinationType.Bolt11));
        Assert.False(DestinationTypes.IsSecretKeyAddress(DestinationType.ArkAddress));
        Assert.False(DestinationTypes.IsSecretKeyAddress(DestinationType.SilentPayment));
    }

    [Fact]
    public void DestinationType_HasEighteenMembers_AndNoUsdcTon()
    {
        Assert.Equal(18, Enum.GetValues<DestinationType>().Length);
        Assert.Null(DestinationTypeConverter.TryParse("usdc_ton"));
    }

    [Theory]
    [InlineData(DestinationType.UsdtEthereum, "usdt_ethereum")]
    [InlineData(DestinationType.UsdcEthereum, "usdc_ethereum")]
    [InlineData(DestinationType.UsdtBsc, "usdt_bsc")]
    [InlineData(DestinationType.UsdcBsc, "usdc_bsc")]
    [InlineData(DestinationType.UsdtBase, "usdt_base")]
    [InlineData(DestinationType.UsdcBase, "usdc_base")]
    [InlineData(DestinationType.UsdtArbitrum, "usdt_arbitrum")]
    [InlineData(DestinationType.UsdcArbitrum, "usdc_arbitrum")]
    [InlineData(DestinationType.UsdtTron, "usdt_tron")]
    [InlineData(DestinationType.UsdtTon, "usdt_ton")]
    [InlineData(DestinationType.UsdtSolana, "usdt_solana")]
    [InlineData(DestinationType.UsdcSolana, "usdc_solana")]
    public void StableTypes_SerializeToWireNames(DestinationType type, string wire)
    {
        Assert.Equal($"\"{wire}\"", System.Text.Json.JsonSerializer.Serialize(type));
        Assert.Equal(type, System.Text.Json.JsonSerializer.Deserialize<DestinationType>($"\"{wire}\""));
    }
}
