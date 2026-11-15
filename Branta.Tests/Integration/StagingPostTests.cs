using Branta;

namespace Branta.Tests.Integration;

public class StagingPostTests
{
    private readonly IBrantaService _service = ServiceFactory.Create(BrantaEnvironment.Staging);

    [LivePostFact]
    public async Task IsApiKeyValid()
    {
        Assert.True(await _service.IsApiKeyValidAsync());
    }

    [LivePostFact]
    public async Task Bitcoin_PostEncryptsAndVerifies()
    {
        var stamp = Guid.NewGuid().ToString("N")[..12];
        var bitcoin = $"bcrt1q{stamp}dotnetlive00";
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .SetDescription($"dotnet-4.0 post bitcoin {stamp}")
            .AddDestination(bitcoin, DestinationType.BitcoinAddress)
            .SetTtl(600)
            .Build());

        Assert.NotEqual(bitcoin, posted.Payment.Destinations[0].Value);

        var enc = Uri.EscapeDataString(posted.Payment.Destinations[0].Value);
        var match = await _service.VerifyAsync($"bitcoin:{bitcoin}?branta_id={enc}&branta_secret={posted.Secret}");
        Assert.Equal(VerifyStatus.Match, match.Status);

        var tampered = await _service.VerifyAsync($"bitcoin:bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4?branta_id={enc}&branta_secret={posted.Secret}");
        Assert.Equal(VerifyStatus.Tampered, tampered.Status);

        var wrongSecret = await _service.VerifyAsync($"bitcoin:{bitcoin}?branta_id={enc}&branta_secret={Guid.NewGuid()}");
        Assert.Equal(VerifyStatus.Unknown, wrongSecret.Status);

        var bare = await _service.VerifyAsync($"bitcoin:{bitcoin}");
        Assert.Equal(VerifyStatus.Unknown, bare.Status);
    }

    [LivePostFact]
    public async Task Bolt11_PostEncryptsAndVerifies()
    {
        var stamp = Guid.NewGuid().ToString("N")[..12];
        var bolt11 = $"lnbc100n1p{stamp}dotnetlive";
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .SetDescription($"dotnet-4.0 post bolt11 {stamp}")
            .AddDestination(bolt11, DestinationType.Bolt11)
            .SetTtl(600)
            .Build());

        Assert.NotEqual(bolt11, posted.Payment.Destinations[0].Value);
        Assert.Equal(VerifyStatus.Match, (await _service.VerifyAsync(bolt11)).Status);
        Assert.Equal(VerifyStatus.Match, (await _service.VerifyAsync($"lightning:{bolt11}")).Status);
    }

    [LivePostFact]
    public async Task Ark_PostEncryptsAndVerifies()
    {
        var stamp = Guid.NewGuid().ToString("N")[..12];
        var ark = $"ark1q{stamp}dotnetliveaddr";
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .SetDescription($"dotnet-4.0 post ark {stamp}")
            .AddDestination(ark, DestinationType.ArkAddress)
            .SetTtl(600)
            .Build());

        Assert.NotEqual(ark, posted.Payment.Destinations[0].Value);
        Assert.Equal(VerifyStatus.Match, (await _service.VerifyAsync(ark)).Status);
    }

    [LivePostFact]
    public async Task SilentPayment_PostEncryptsAndVerifies()
    {
        var stamp = Guid.NewGuid().ToString("N")[..12];
        var silent = $"sp1q{stamp}dotnetlivesp00";
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .SetDescription($"dotnet-4.0 post silent {stamp}")
            .AddDestination(silent, DestinationType.SilentPayment)
            .SetTtl(600)
            .Build());

        Assert.NotEqual(silent, posted.Payment.Destinations[0].Value);
        Assert.Equal(VerifyStatus.Match, (await _service.VerifyAsync(silent)).Status);
    }

    [LivePostFact]
    public async Task Unified_PostEncryptsAndVerifies()
    {
        var stamp = Guid.NewGuid().ToString("N")[..12];
        var bitcoin = $"bcrt1q{stamp}dotnetlive0u";
        var bolt11 = $"lnbc100n1p{stamp}dotnetliveu";
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .SetDescription($"dotnet-4.0 post unified {stamp}")
            .AddDestination(bitcoin, DestinationType.BitcoinAddress)
            .AddDestination(bolt11, DestinationType.Bolt11)
            .SetTtl(600)
            .Build());

        var enc = posted.Payment.Destinations.First(d => d.Type == DestinationType.BitcoinAddress).Value;
        var qr = $"bitcoin:{bitcoin}?lightning={bolt11}&branta_id={Uri.EscapeDataString(enc)}&branta_secret={posted.Secret}";
        Assert.Equal(VerifyStatus.Match, (await _service.VerifyAsync(qr)).Status);
    }

    private const string Deposit = "0xAbCdEf0123456789aBcDeF0123456789AbCdEf01";

    private static string TransferUri(DestinationType type, string encrypted, string secret)
    {
        var rail = DestinationTypes.EvmTokenRails.Single(r => r.Type == type);
        return $"ethereum:{rail.Contract}@{rail.ChainId}/transfer?address={Deposit}&uint256=50000" +
               $"&branta_id={Uri.EscapeDataString(encrypted)}&branta_secret={secret}";
    }

    [LivePostFact]
    public async Task Erc681_DualTokenSameDeposit_BindsOnlyTheScannedToken()
    {
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .AddDestination(Deposit, DestinationType.UsdtBase)
            .AddDestination(Deposit, DestinationType.UsdcBase)
            .SetTtl(120)
            .Build());

        foreach (var type in new[] { DestinationType.UsdtBase, DestinationType.UsdcBase })
        {
            var enc = posted.Payment.Destinations.First(d => d.Type == type).Value;
            var result = await _service.VerifyAsync(TransferUri(type, enc, posted.Secret));
            Assert.Equal(VerifyStatus.Match, result.Status);
            Assert.Single(result.MatchedDestinations!);
            Assert.Equal(type, result.MatchedDestinations![0].Type);
        }
    }

    [LivePostFact]
    public async Task Erc681_UsdcScanAgainstUsdtOnlyPayment_IsUnknown()
    {
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .AddDestination(Deposit, DestinationType.UsdtBase)
            .SetTtl(120)
            .Build());

        var result = await _service.VerifyAsync(
            TransferUri(DestinationType.UsdcBase, posted.Payment.Destinations[0].Value, posted.Secret));
        Assert.Equal(VerifyStatus.Unknown, result.Status);
    }

    [LivePostFact]
    public async Task Erc681_BitcoinAndEvmInOnePayment_EachScanBindsItsOwnRail()
    {
        const string bitcoin = "bc1qgw3dzmhnyvcswc9r0v0z0ajtp8ulm4nuyeahwr";
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .AddDestination(bitcoin, DestinationType.BitcoinAddress)
            .AddDestination(Deposit, DestinationType.UsdtPolygon)
            .SetTtl(120)
            .Build());

        var btcEnc = posted.Payment.Destinations.First(d => d.Type == DestinationType.BitcoinAddress).Value;
        var evmEnc = posted.Payment.Destinations.First(d => d.Type == DestinationType.UsdtPolygon).Value;

        var btc = await _service.VerifyAsync(
            $"bitcoin:{bitcoin}?branta_id={Uri.EscapeDataString(btcEnc)}&branta_secret={posted.Secret}");
        Assert.Equal(VerifyStatus.Match, btc.Status);
        Assert.Single(btc.MatchedDestinations!);
        Assert.Equal(DestinationType.BitcoinAddress, btc.MatchedDestinations![0].Type);

        var evm = await _service.VerifyAsync(TransferUri(DestinationType.UsdtPolygon, evmEnc, posted.Secret));
        Assert.Equal(VerifyStatus.Match, evm.Status);
        Assert.Single(evm.MatchedDestinations!);
        Assert.Equal(DestinationType.UsdtPolygon, evm.MatchedDestinations![0].Type);
    }
}
