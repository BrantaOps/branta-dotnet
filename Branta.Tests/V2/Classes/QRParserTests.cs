using Branta.Enums;
using Branta.V2.Classes;

namespace Branta.Tests.V2.Classes;

public class QRParserTests
{
    [Fact]
    public void QRParser_BitcoinUri_SetsBitcoinAddressTypeAndDestination()
    {
        var result = new QRParser("bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa");

        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
        Assert.Null(result.OnChainEncryptionText);
        Assert.Null(result.OnChainEncryptionSecret);
    }

    [Fact]
    public void QRParser_BitcoinUriWithBrantaParams_SetsZkProperties()
    {
        var result = new QRParser("bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa?branta_id=encrypted-bitcoin-address&branta_secret=1234");

        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
        Assert.Equal("encrypted-bitcoin-address", result.OnChainEncryptionText);
        Assert.Equal("1234", result.OnChainEncryptionSecret);
    }

    [Fact]
    public void QRParser_PlainBitcoinAddress_SetsBitcoinAddressType()
    {
        var result = new QRParser("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa");

        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
    }

    [Fact]
    public void QRParser_LightningBolt11Uri_SetsBolt11Type()
    {
        var result = new QRParser("lightning:lnbc100n1ptest");

        Assert.Equal("lnbc100n1ptest", result.Destination);
        Assert.Equal(DestinationType.Bolt11, result.DestinationType);
    }

    [Fact]
    public void QRParser_PlainBolt11_SetsBolt11Type()
    {
        var result = new QRParser("lnbc100n1ptest");

        Assert.Equal("lnbc100n1ptest", result.Destination);
        Assert.Equal(DestinationType.Bolt11, result.DestinationType);
    }

    [Fact]
    public void QRParser_LightningBolt12Uri_SetsBolt12Type()
    {
        var result = new QRParser("lightning:lno1qcptest");

        Assert.Equal("lno1qcptest", result.Destination);
        Assert.Equal(DestinationType.Bolt12, result.DestinationType);
    }

    [Fact]
    public void QRParser_PlainBolt12_SetsBolt12Type()
    {
        var result = new QRParser("lno1qcptest");

        Assert.Equal("lno1qcptest", result.Destination);
        Assert.Equal(DestinationType.Bolt12, result.DestinationType);
    }

    [Fact]
    public void QRParser_LightningLnUrlUri_SetsLnUrlType()
    {
        var result = new QRParser("lightning:LNURL1DP68GURN8GHJ");

        Assert.Equal("LNURL1DP68GURN8GHJ", result.Destination);
        Assert.Equal(DestinationType.LnUrl, result.DestinationType);
    }

    [Fact]
    public void QRParser_PlainLnUrl_SetsLnUrlType()
    {
        var result = new QRParser("LNURL1DP68GURN8GHJ");

        Assert.Equal("LNURL1DP68GURN8GHJ", result.Destination);
        Assert.Equal(DestinationType.LnUrl, result.DestinationType);
    }

    [Fact]
    public void QRParser_EthereumAddress_SetsTetherAddressType()
    {
        var result = new QRParser("0x742d35Cc6634C0532925a3b844Bc454e4438f44e");

        Assert.Equal("0x742d35Cc6634C0532925a3b844Bc454e4438f44e", result.Destination);
        Assert.Equal(DestinationType.TetherAddress, result.DestinationType);
    }

    [Fact]
    public void QRParser_TronAddress_SetsTetherAddressType()
    {
        var result = new QRParser("TJmUNSGV6b1CCVXN1KkABY49nUJGWDH3Hd");

        Assert.Equal("TJmUNSGV6b1CCVXN1KkABY49nUJGWDH3Hd", result.Destination);
        Assert.Equal(DestinationType.TetherAddress, result.DestinationType);
    }

    [Fact]
    public void QRParser_ArkAddress_SetsArkAddressType()
    {
        var result = new QRParser("ark1qqjqtest");

        Assert.Equal("ark1qqjqtest", result.Destination);
        Assert.Equal(DestinationType.ArkAddress, result.DestinationType);
    }

    [Fact]
    public void QRParser_UnrecognizedText_SetsNullType()
    {
        var result = new QRParser("not-any-known-format");

        Assert.Equal("not-any-known-format", result.Destination);
        Assert.Null(result.DestinationType);
    }

    [Fact]
    public void QRParser_LeadingTrailingWhitespace_IsTrimmed()
    {
        var result = new QRParser("  lnbc100n1ptest  ");

        Assert.Equal("lnbc100n1ptest", result.Destination);
        Assert.Equal(DestinationType.Bolt11, result.DestinationType);
    }

    [Fact]
    public void QRParser_CombinedQR_ShouldParse()
    {
        var result = new QRParser("bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa?&lightning=lnbc100n1ptest");

        Assert.Equal(2, result.Destinations.Count);
        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
        Assert.Equal("lnbc100n1ptest", result.Destinations[1].Value);
        Assert.Equal(DestinationType.Bolt11, result.Destinations[1].Type);
        Assert.False(result.IsOnChainZk());
    }

    [Fact]
    public void QRParser_CombinedQRWithMultipleAlts_ShouldParse()
    {
        var result = new QRParser("bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa?&lightning=lnbc100n1ptest&ark=ark100testaddress");

        Assert.Equal(3, result.Destinations.Count);
        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
        Assert.Equal("lnbc100n1ptest", result.Destinations[1].Value);
        Assert.Equal(DestinationType.Bolt11, result.Destinations[1].Type);
        Assert.Equal("ark100testaddress", result.Destinations[2].Value);
        Assert.Equal(DestinationType.ArkAddress, result.Destinations[2].Type);
        Assert.False(result.IsOnChainZk());
    }
}
