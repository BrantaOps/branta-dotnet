using Branta;

namespace Branta.Tests.Classes;

public class QRParserTests
{
    [Fact]
    public void QRParser_BitcoinUri_SetsBitcoinAddressTypeAndDestination()
    {
        var result = new QRParser("bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa");

        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
        Assert.True(result.Destinations[0].SecretKey);
        Assert.Equal([DestinationType.BitcoinAddress], result.ExpectedSecretKeyTypes);
        Assert.Null(result.OnChainEncryptionText);
        Assert.Null(result.OnChainEncryptionSecret);
    }

    [Fact]
    public void QRParser_PolUri_IsAddressOnlyAndMapsToPolygonEvmFamily()
    {
        const string address = "0x278297528c32eCa01295B0d2135984d3442ec963";
        var result = new QRParser(
            $"pol:{address}?value=50&user=send&currency=USD&branta_id=abc%2Bdef%3D&branta_secret=1234");

        Assert.Equal(address, result.Destination);
        Assert.Null(result.DestinationType);
        Assert.True(result.Destinations[0].SecretKey);
        Assert.Equal(DestinationTypes.PolygonEvm, result.ExpectedSecretKeyTypes);
        Assert.Equal("abc+def=", result.OnChainEncryptionText);
        Assert.Equal("1234", result.OnChainEncryptionSecret);
    }

    [Theory]
    [InlineData("eth")]
    [InlineData("bsc")]
    [InlineData("tron")]
    [InlineData("ton")]
    [InlineData("sol")]
    public void QRParser_ChainUri_IsAddressOnlyAndMapsToSchemeFamily(string scheme)
    {
        var result = new QRParser($"{scheme}:someaddress?value=1&branta_id=enc&branta_secret=sec");

        Assert.Equal("someaddress", result.Destination);
        Assert.Null(result.DestinationType);
        Assert.True(result.Destinations[0].SecretKey);
        Assert.Equal(DestinationTypes.SecretKeyUriFamilies[scheme], result.ExpectedSecretKeyTypes);
        Assert.Equal("enc", result.OnChainEncryptionText);
        Assert.Equal("sec", result.OnChainEncryptionSecret);
    }

    [Fact]
    public void QRParser_PolUriWithoutQuery_StillMapsToPolygonEvmFamily()
    {
        const string address = "0x278297528c32eCa01295B0d2135984d3442ec963";
        var result = new QRParser($"pol:{address}");

        Assert.Equal(address, result.Destination);
        Assert.Null(result.DestinationType);
        Assert.Equal(DestinationTypes.PolygonEvm, result.ExpectedSecretKeyTypes);
    }

    [Fact]
    public void QRParser_BitcoinUriWithBrantaParams_SetsBrantaParams()
    {
        var result = new QRParser("bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa?branta_id=abc%2Bdef%3D&branta_secret=1234");

        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
        Assert.Equal("abc+def=", result.OnChainEncryptionText);
        Assert.Equal("1234", result.OnChainEncryptionSecret);
        Assert.True(result.HasBrantaParams());
    }

    [Fact]
    public void QRParser_BitcoinUri_DecodesUriEncodedLightningParam()
    {
        var result = new QRParser("bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa?lightning=lnbc100n1ptest%3Dpadded");

        Assert.Equal(2, result.Destinations.Count);
        Assert.Equal("lnbc100n1ptest=padded", result.Destinations[1].Value);
        Assert.Equal(DestinationType.Bolt11, result.Destinations[1].Type);
    }

    [Fact]
    public void QRParser_PlainBitcoinAddress_SetsBitcoinAddressType()
    {
        var result = new QRParser("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa");

        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
    }

    [Fact]
    public void QRParser_TestnetBech32_SetsBitcoinAddressType()
    {
        var result = new QRParser("tb1qtest");

        Assert.Equal("tb1qtest", result.Destination);
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
    public void QRParser_LightningBolt12Uri_IsUnverifiable()
    {
        var result = new QRParser("lightning:lno1qcptest");

        Assert.Equal("lno1qcptest", result.Destination);
        Assert.Null(result.DestinationType);
        Assert.True(result.HasUnverifiableInstruction);
    }

    [Fact]
    public void QRParser_PlainBolt12_HasNoType()
    {
        var result = new QRParser("lno1qcptest");

        Assert.Equal("lno1qcptest", result.Destination);
        Assert.Null(result.DestinationType);
        Assert.False(result.HasUnverifiableInstruction);
    }

    [Fact]
    public void QRParser_ArkAddress_SetsArkAddressType()
    {
        var result = new QRParser("ark1qqjqtest");

        Assert.Equal("ark1qqjqtest", result.Destination);
        Assert.Equal(DestinationType.ArkAddress, result.DestinationType);
    }

    [Fact]
    public void QRParser_SignetArkAddress_SetsArkAddressType()
    {
        var result = new QRParser("tark1qqjqtest");

        Assert.Equal("tark1qqjqtest", result.Destination);
        Assert.Equal(DestinationType.ArkAddress, result.DestinationType);
    }

    [Fact]
    public void QRParser_SilentPaymentAddress_SetsSilentPaymentType()
    {
        var result = new QRParser("sp1qqwl5p9jhz0000h5zkvlf9gfqv9dl9qjp5ggq5x3fw");

        Assert.Equal("sp1qqwl5p9jhz0000h5zkvlf9gfqv9dl9qjp5ggq5x3fw", result.Destination);
        Assert.Equal(DestinationType.SilentPayment, result.DestinationType);
    }

    [Fact]
    public void QRParser_TestnetSilentPaymentAddress_SetsSilentPaymentType()
    {
        var result = new QRParser("tsp1qqwl5p9jhz0000h5zkvlf9gfqv9dl9qjp5ggq5x3fw");

        Assert.Equal("tsp1qqwl5p9jhz0000h5zkvlf9gfqv9dl9qjp5ggq5x3fw", result.Destination);
        Assert.Equal(DestinationType.SilentPayment, result.DestinationType);
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
        Assert.False(result.HasBrantaParams());
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
        Assert.False(result.HasBrantaParams());
    }

    [Fact]
    public void QRParser_BitcoinUriWithSilentPaymentParam_AddsSilentPaymentDestination()
    {
        var result = new QRParser("bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa?silent_payment=sp1qqwl5p9jhz0000h5zkvlf9gfqv9dl9qjp5ggq5x3fw");

        Assert.Equal(2, result.Destinations.Count);
        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
        Assert.Equal("sp1qqwl5p9jhz0000h5zkvlf9gfqv9dl9qjp5ggq5x3fw", result.Destinations[1].Value);
        Assert.Equal(DestinationType.SilentPayment, result.Destinations[1].Type);
    }

    [Fact]
    public void QRParser_Bip321BcAndTb_AreOnChainAddresses()
    {
        var result = new QRParser("bitcoin:?bc=bc1qmerchant&tb=tb1qtest");

        Assert.Equal("", result.Destination);
        Assert.Equal(new[] { "", "bc1qmerchant", "tb1qtest" }, result.Destinations.Select(d => d.Value));
        Assert.Equal(DestinationType.BitcoinAddress, result.Destinations[1].Type);
        Assert.Equal(DestinationType.BitcoinAddress, result.Destinations[2].Type);
        Assert.False(result.HasUnverifiableInstruction);
    }

    [Fact]
    public void QRParser_SpParam_AddsSilentPaymentDestination()
    {
        var result = new QRParser("bitcoin:bc1qmerchant?sp=sp1qqexample");

        Assert.Equal("sp1qqexample", result.Destinations[1].Value);
        Assert.Equal(DestinationType.SilentPayment, result.Destinations[1].Type);
        Assert.False(result.HasUnverifiableInstruction);
    }

    [Fact]
    public void QRParser_LnoAndPay_AreUnverifiableAndNotAdded()
    {
        var lno = new QRParser("bitcoin:bc1qmerchant?lno=lno1qqexample");
        Assert.True(lno.HasUnverifiableInstruction);
        Assert.Equal(new[] { "bc1qmerchant" }, lno.Destinations.Select(d => d.Value));

        var pay = new QRParser("bitcoin:bc1qmerchant?pay=bitcoin:bc1qother");
        Assert.True(pay.HasUnverifiableInstruction);
        Assert.Equal(new[] { "bc1qmerchant" }, pay.Destinations.Select(d => d.Value));

        var bolt12 = new QRParser("bitcoin:bc1qmerchant?bolt12=lno1qqexample");
        Assert.True(bolt12.HasUnverifiableInstruction);
        Assert.Equal(new[] { "bc1qmerchant" }, bolt12.Destinations.Select(d => d.Value));
    }

    [Fact]
    public void QRParser_LightningLnurlUri_IsUnverifiable()
    {
        var result = new QRParser("lightning:LNURL1DP68GURN8GHJ");

        Assert.Equal("LNURL1DP68GURN8GHJ", result.Destination);
        Assert.Null(result.DestinationType);
        Assert.True(result.HasUnverifiableInstruction);
    }

    [Fact]
    public void QRParser_TabInsideAddress_UsesAddressWithoutTab()
    {
        var result = new QRParser("bitcoin:1attac\tkeraddr?branta_id=ID1&branta_secret=SEC1");

        Assert.Equal("1attackeraddr", result.Destination);
        Assert.Equal(DestinationType.BitcoinAddress, result.DestinationType);
        Assert.True(result.HasBrantaParams());
    }

    [Fact]
    public void QRParser_TabInsideLightningScheme_StillTypesBolt11()
    {
        var result = new QRParser("lightnin\tg:lnbc100n1ptest");

        Assert.Equal("lnbc100n1ptest", result.Destination);
        Assert.Equal(DestinationType.Bolt11, result.DestinationType);
    }
}
