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

    [LivePostFact]
    public async Task UsdtPolygon_PostEncryptsAndVerifies()
    {
        const string address = "0x278297528c32eCa01295B0d2135984d3442ec963";
        const string swapped = "0x1111111111111111111111111111111111111111";
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .SetDescription($"dotnet usdt_polygon {Guid.NewGuid():N}")
            .AddDestination(address, DestinationType.UsdtPolygon)
            .SetTtl(120)
            .Build());

        Assert.Equal(DestinationType.UsdtPolygon, posted.Payment.Destinations[0].Type);
        var enc = Uri.EscapeDataString(posted.Payment.Destinations[0].Value);
        var match = await _service.VerifyAsync(
            $"pol:{address}?value=50&currency=USD&branta_id={enc}&branta_secret={posted.Secret}");
        Assert.Equal(VerifyStatus.Match, match.Status);
        Assert.NotNull(match.MatchedDestinations);
        Assert.Single(match.MatchedDestinations!);
        Assert.Equal(DestinationType.UsdtPolygon, match.MatchedDestinations[0].Type);

        Assert.Equal(VerifyStatus.Match, (await _service.VerifyAsync(
            $"pol:{address.ToLowerInvariant()}?branta_id={enc}&branta_secret={posted.Secret}")).Status);
        Assert.Equal(VerifyStatus.Tampered, (await _service.VerifyAsync(
            $"pol:{swapped}?branta_id={enc}&branta_secret={posted.Secret}")).Status);
        Assert.Equal(VerifyStatus.Unknown, (await _service.VerifyAsync(address)).Status);
        Assert.Equal(VerifyStatus.Unknown, (await _service.VerifyAsync($"pol:{address}?value=50")).Status);
    }

    [LivePostFact]
    public async Task UsdcPolygon_PostEncryptsAndVerifies()
    {
        const string address = "0xAb5801a7D398351b8bE11C439e05C5B3259aeC9B";
        const string swapped = "0x1111111111111111111111111111111111111111";
        var posted = await _service.AddPaymentAsync(new PaymentBuilder()
            .SetDescription($"dotnet usdc_polygon {Guid.NewGuid():N}")
            .AddDestination(address, DestinationType.UsdcPolygon)
            .SetTtl(120)
            .Build());

        Assert.Equal(DestinationType.UsdcPolygon, posted.Payment.Destinations[0].Type);
        var enc = Uri.EscapeDataString(posted.Payment.Destinations[0].Value);
        var match = await _service.VerifyAsync(
            $"pol:{address}?value=50&currency=USD&branta_id={enc}&branta_secret={posted.Secret}");
        Assert.Equal(VerifyStatus.Match, match.Status);
        Assert.NotNull(match.MatchedDestinations);
        Assert.Single(match.MatchedDestinations!);
        Assert.Equal(DestinationType.UsdcPolygon, match.MatchedDestinations[0].Type);

        Assert.Equal(VerifyStatus.Match, (await _service.VerifyAsync(
            $"pol:{address.ToLowerInvariant()}?branta_id={enc}&branta_secret={posted.Secret}")).Status);
        Assert.Equal(VerifyStatus.Tampered, (await _service.VerifyAsync(
            $"pol:{swapped}?branta_id={enc}&branta_secret={posted.Secret}")).Status);
        Assert.Equal(VerifyStatus.Unknown, (await _service.VerifyAsync(address)).Status);
        Assert.Equal(VerifyStatus.Unknown, (await _service.VerifyAsync($"pol:{address}?value=50")).Status);
    }
}
