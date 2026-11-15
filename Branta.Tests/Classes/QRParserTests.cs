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

    private const string BaseUsdc = "0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913";
    private const string Deposit = "0x1111111111111111111111111111111111111111";

    public static IEnumerable<object[]> EvmRails() =>
        DestinationTypes.EvmTokenRails.Select(r => new object[] { r.Type });

    [Fact]
    public void QRParser_Erc681Transfer_BindsTheDepositAndOneToken()
    {
        var result = new QRParser(
            $"ethereum:{BaseUsdc}@8453/transfer?address={Deposit}&uint256=1000000&branta_id=abc%2Bdef%3D&branta_secret=1234");

        Assert.Equal(Deposit, result.Destination);
        Assert.Single(result.Destinations);
        Assert.Equal(DestinationType.UsdcBase, result.DestinationType);
        Assert.True(result.Destinations[0].SecretKey);
        Assert.Equal([DestinationType.UsdcBase], result.ExpectedSecretKeyTypes);
        Assert.Equal("abc+def=", result.OnChainEncryptionText);
        Assert.Equal("1234", result.OnChainEncryptionSecret);
    }

    [Theory]
    [MemberData(nameof(EvmRails))]
    public void QRParser_Erc681_EveryRailBindsItsOwnType(DestinationType type)
    {
        var rail = DestinationTypes.EvmTokenRails.Single(r => r.Type == type);
        var result = new QRParser(
            $"ethereum:{rail.Contract}@{rail.ChainId}/transfer?address={Deposit}&branta_id=a&branta_secret=b");

        Assert.Equal(Deposit, result.Destination);
        Assert.Equal(type, result.DestinationType);
        Assert.Equal([type], result.ExpectedSecretKeyTypes);
    }

    [Fact]
    public void QRParser_Erc681_ContractMatchesCaseInsensitively()
    {
        var result = new QRParser($"ethereum:{BaseUsdc.ToLowerInvariant()}@8453/transfer?address={Deposit}");

        Assert.Equal(DestinationType.UsdcBase, result.DestinationType);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("50000000")]
    [InlineData("999999999999999999999")]
    public void QRParser_Erc681_Uint256IsNotPartOfTheBinding(string amount)
    {
        var result = new QRParser($"ethereum:{BaseUsdc}@8453/transfer?address={Deposit}&uint256={amount}");

        Assert.Equal(Deposit, result.Destination);
        Assert.Equal(DestinationType.UsdcBase, result.DestinationType);
    }

    [Theory]
    [InlineData("ethereum:/{0}@8453/transfer?address={1}")]
    [InlineData("ETHEREUM:{0}@8453/transfer?address={1}")]
    public void QRParser_Erc681_ToleratesOneLeadingSlashAndUpperCaseScheme(string template)
    {
        var result = new QRParser(string.Format(template, BaseUsdc, Deposit));

        Assert.Equal(DestinationType.UsdcBase, result.DestinationType);
    }

    [Fact]
    public void QRParser_Erc681_AcceptsUpperCase0xDepositPrefix()
    {
        var result = new QRParser($"ethereum:{BaseUsdc}@8453/transfer?address=0X{Deposit[2..]}");

        Assert.Equal(DestinationType.UsdcBase, result.DestinationType);
    }

    [Fact]
    public void QRParser_Erc681_FirstAddressParamWins()
    {
        var result = new QRParser($"ethereum:{BaseUsdc}@8453/transfer?address={Deposit}&address=0x{new string('f', 40)}");

        Assert.Equal(Deposit, result.Destination);
    }

    [Fact]
    public void QRParser_Erc681_PercentEncodedAmpersandStaysInsideBrantaId()
    {
        var result = new QRParser(
            $"ethereum:{BaseUsdc}@8453/transfer?address={Deposit}&branta_id=a%26b&branta_secret=s");

        Assert.Equal("a&b", result.OnChainEncryptionText);
        Assert.Equal("s", result.OnChainEncryptionSecret);
    }

    [Fact]
    public void QRParser_Erc681_ParamsInTheFragmentAreNotRead()
    {
        var result = new QRParser(
            $"ethereum:{BaseUsdc}@8453/transfer?address={Deposit}#&branta_id=abc&branta_secret=1234");

        Assert.Equal(DestinationType.UsdcBase, result.DestinationType);
        Assert.Null(result.OnChainEncryptionText);
        Assert.False(result.HasBrantaParams());
    }

    [Fact]
    public void QRParser_Erc681_AQueryAfterAFragmentIsNotRead()
    {
        var result = new QRParser(
            $"ethereum:{BaseUsdc}@8453/transfer#x?address={Deposit}&branta_id=a&branta_secret=b");

        Assert.Null(result.DestinationType);
        Assert.Null(result.OnChainEncryptionText);
        Assert.Null(result.ExpectedSecretKeyTypes);
    }

    [Fact]
    public void QRParser_Erc681_AFragmentAfterTheQueryIsDropped()
    {
        var result = new QRParser(
            $"ethereum:{BaseUsdc}@8453/transfer?address={Deposit}&branta_id=a&branta_secret=b#address=0x2");

        Assert.Equal(Deposit, result.Destination);
        Assert.Equal("a", result.OnChainEncryptionText);
        Assert.Equal("b", result.OnChainEncryptionSecret);
    }

    [Fact]
    public void QRParser_BitcoinQueryAfterAFragmentIsNotRead()
    {
        var afterFragment = new QRParser("bitcoin:bc1qtest#x?branta_id=a&branta_secret=b");
        Assert.Null(afterFragment.OnChainEncryptionText);

        var beforeFragment = new QRParser("bitcoin:bc1qtest?branta_id=a&branta_secret=b#frag");
        Assert.Equal("b", beforeFragment.OnChainEncryptionSecret);
    }

    [Fact]
    public void QRParser_Erc681_ChainIdBeyond15DigitsIsUntyped()
    {
        var result = new QRParser($"ethereum:{BaseUsdc}@{new string('9', 5000)}/transfer?address={Deposit}");

        Assert.Null(result.DestinationType);
    }

    [Fact]
    public void QRParser_Erc681_LeadingZeroChainIdStillSelectsTheRail()
    {
        var result = new QRParser($"ethereum:{BaseUsdc}@{new string('0', 4500)}8453/transfer?address={Deposit}");

        Assert.Equal(DestinationType.UsdcBase, result.DestinationType);
    }

    public static IEnumerable<object[]> UnsupportedErc681Forms()
    {
        var tail = $"address={Deposit}&branta_id=abc&branta_secret=1234";
        string[] forms =
        [
            // No chain id.
            $"ethereum:{BaseUsdc}/transfer?{tail}",
            // Payment-request prefix and ENS-style forms.
            $"ethereum:pay-{BaseUsdc}@8453/transfer?{tail}",
            $"ethereum:vitalik.eth@8453/transfer?{tail}",
            // // makes the contract an authority, not a path.
            $"ethereum://{BaseUsdc}@8453/transfer?{tail}",
            // Only transfer is a payment.
            $"ethereum:{BaseUsdc}@8453/approve?{tail}",
            $"ethereum:{BaseUsdc}@8453/transferFrom?{tail}",
            // Path parameters after the function name.
            $"ethereum:{BaseUsdc}@8453/transfer;x=1?{tail}",
            // Chain id forms that are not plain digits.
            $"ethereum:{BaseUsdc}@0x2105/transfer?{tail}",
            $"ethereum:{BaseUsdc}@-8453/transfer?{tail}",
            $"ethereum:{BaseUsdc}@+8453/transfer?{tail}",
            $"ethereum:{BaseUsdc}@/transfer?{tail}",
            // No address, a truncated address, or an ENS recipient.
            $"ethereum:{BaseUsdc}@8453/transfer?branta_id=abc&branta_secret=1234",
            $"ethereum:{BaseUsdc}@8453/transfer?address=0x1234&branta_id=abc&branta_secret=1234",
            $"ethereum:{BaseUsdc}@8453/transfer?address=vitalik.eth&branta_id=abc&branta_secret=1234",
            // Unknown contract and the right contract on the wrong chain.
            $"ethereum:0x{new string('2', 40)}@8453/transfer?{tail}",
            $"ethereum:{BaseUsdc}@1/transfer?{tail}",
        ];
        return forms.Select(f => new object[] { f });
    }

    [Theory]
    [MemberData(nameof(UnsupportedErc681Forms))]
    public void QRParser_Erc681_UnsupportedFormsDoNotBind(string qr)
    {
        var result = new QRParser(qr);

        Assert.Null(result.DestinationType);
        Assert.Null(result.ExpectedSecretKeyTypes);
        Assert.Null(result.OnChainEncryptionText);
        Assert.False(result.Destinations[0].SecretKey);
    }

    [Fact]
    public void QRParser_UnparseableOrNonAsciiInput_IsAnUntypedDestination()
    {
        var bad = new QRParser("ethereum://[bad/transfer");
        Assert.Equal("ethereum://[bad/transfer", bad.Destination);
        Assert.Null(bad.DestinationType);

        foreach (var text in new[] { "ethereum:é@8453/transfer?address=é", "ethereum:0xé@é/transfer#é?é", "ethereum:\U0001F600@\U0001F600/transfer" })
            Assert.Null(new QRParser(text).DestinationType);

        Assert.Null(new QRParser("bitcoin://[bad").OnChainEncryptionText);
    }

    [Theory]
    [InlineData("pol")]
    [InlineData("eth")]
    [InlineData("bsc")]
    [InlineData("tron")]
    [InlineData("ton")]
    [InlineData("sol")]
    public void QRParser_RemovedChainSchemes_DoNotBind(string scheme)
    {
        var qr = $"{scheme}:0x278297528c32eCa01295B0d2135984d3442ec963?value=50&branta_id=abc&branta_secret=1234";
        var result = new QRParser(qr);

        Assert.Equal(qr, result.Destination);
        Assert.Null(result.DestinationType);
        Assert.False(result.Destinations[0].SecretKey);
        Assert.Null(result.ExpectedSecretKeyTypes);
        Assert.False(result.HasBrantaParams());
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
