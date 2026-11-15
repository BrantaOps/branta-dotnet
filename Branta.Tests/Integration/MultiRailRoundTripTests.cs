using Branta;

namespace Branta.Tests.Integration;

/// <summary>
/// Live staging AddPaymentAsync then VerifyAsync for every secret-key rail.
/// Gated like StagingPostTests (BRANTA_RUN_INTEGRATION=1 + BRANTA_API_KEY).
/// </summary>
public class MultiRailRoundTripTests
{
    private const string Deposit = "0xAbCdEf0123456789aBcDeF0123456789AbCdEf01";
    private const string SwappedDeposit = "0x1111111111111111111111111111111111111111";

    private readonly IBrantaService _service = ServiceFactory.Create(BrantaEnvironment.Staging);

    public static IEnumerable<object[]> EvmRails() =>
        DestinationTypes.EvmTokenRails.Select(r => new object[] { r.Type });

    public static IEnumerable<object[]> PostOnlyRails() =>
    [
        [DestinationType.UsdtTron, "T9yD14Nj9j7xAB4dbGeiX9h8unkKHxuWwb"],
        [DestinationType.UsdtTon, "EQDtFpEwcFAEcRe5mLVh2N6C0y-_tur3BYOM93pEZPw390by"],
        [DestinationType.UsdtSolana, "Es9vMFrzaCERmJfrF4H2FYD4KCoNkY11McCe8BenwNYB"],
        [DestinationType.UsdcSolana, "7EqQdEULxWcraVx3mXKFjc84LhCkMGZCkRuDofycyinf"],
    ];

    private static string TransferUri(DestinationType type, string address, string encrypted, string secret)
    {
        var rail = DestinationTypes.EvmTokenRails.Single(r => r.Type == type);
        return $"ethereum:{rail.Contract}@{rail.ChainId}/transfer?address={address}&uint256=50000" +
               $"&branta_id={Uri.EscapeDataString(encrypted)}&branta_secret={Uri.EscapeDataString(secret)}";
    }

    [LivePostTheory]
    [MemberData(nameof(EvmRails))]
    public async Task Staging_EvmRoundTrip(DestinationType type)
    {
        var added = await _service.AddPaymentAsync(new PaymentBuilder()
            .AddDestination(Deposit, type)
            .SetTtl(120)
            .Build());

        var encrypted = added.Payment.Destinations[0].Value;
        Assert.Equal(type, added.Payment.Destinations[0].Type);

        var match = await _service.VerifyAsync(TransferUri(type, Deposit, encrypted, added.Secret));
        Assert.Equal(VerifyStatus.Match, match.Status);
        Assert.Single(match.MatchedDestinations!);
        Assert.Equal(type, match.MatchedDestinations![0].Type);
        Assert.Equal(Deposit, match.MatchedDestinations[0].Value);

        Assert.Equal(
            VerifyStatus.Match,
            (await _service.VerifyAsync(TransferUri(type, Deposit.ToLowerInvariant(), encrypted, added.Secret))).Status);

        var tampered = await _service.VerifyAsync(TransferUri(type, SwappedDeposit, encrypted, added.Secret));
        Assert.Equal(VerifyStatus.Tampered, tampered.Status);
        Assert.DoesNotContain('#', tampered.VerifyUrl);

        Assert.Equal(
            VerifyStatus.Unknown,
            (await _service.VerifyAsync(TransferUri(type, Deposit, encrypted, Guid.NewGuid().ToString()))).Status);
        Assert.Equal(VerifyStatus.Unknown, (await _service.VerifyAsync(Deposit)).Status);
    }

    /// <summary>Tron, TON, and Solana still encrypt on AddPaymentAsync. They have no verify URI.</summary>
    [LivePostTheory]
    [MemberData(nameof(PostOnlyRails))]
    public async Task Staging_PostOnlyRailsEncrypt(DestinationType type, string address)
    {
        var added = await _service.AddPaymentAsync(new PaymentBuilder()
            .AddDestination(address, type)
            .SetTtl(120)
            .Build());

        Assert.Equal(type, added.Payment.Destinations[0].Type);
        Assert.NotEqual(address, added.Payment.Destinations[0].Value);
    }

    [LivePostFact]
    public async Task Staging_BitcoinRoundTrip()
    {
        const string address = "bc1qgw3dzmhnyvcswc9r0v0z0ajtp8ulm4nuyeahwr";
        const string swapped = "bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4";
        var added = await _service.AddPaymentAsync(new PaymentBuilder()
            .AddDestination(address, DestinationType.BitcoinAddress)
            .SetTtl(120)
            .Build());
        var enc = Uri.EscapeDataString(added.Payment.Destinations[0].Value);

        var match = await _service.VerifyAsync($"bitcoin:{address}?branta_id={enc}&branta_secret={added.Secret}");
        Assert.Equal(VerifyStatus.Match, match.Status);
        Assert.Equal(DestinationType.BitcoinAddress, match.MatchedDestinations![0].Type);
        Assert.Equal(
            VerifyStatus.Tampered,
            (await _service.VerifyAsync($"bitcoin:{swapped}?branta_id={enc}&branta_secret={added.Secret}")).Status);
        Assert.Equal(VerifyStatus.Unknown, (await _service.VerifyAsync($"bitcoin:{address}")).Status);
    }
}

internal sealed class LivePostTheoryAttribute : TheoryAttribute
{
    public LivePostTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("BRANTA_RUN_INTEGRATION") != "1")
            Skip = "Live lookups skipped unless BRANTA_RUN_INTEGRATION=1";
        else if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BRANTA_API_KEY")))
            Skip = "Live POST skipped unless BRANTA_API_KEY is set";
    }
}
