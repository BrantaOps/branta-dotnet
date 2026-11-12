using Branta;

namespace Branta.Tests.Integration;

/// <summary>
/// Live staging addPayment → verify for every secret-key rail.
/// Gated like StagingPostTests (BRANTA_RUN_INTEGRATION=1 + BRANTA_API_KEY).
/// </summary>
public class MultiRailRoundTripTests
{
    private readonly IBrantaService _service = ServiceFactory.Create(BrantaEnvironment.Staging);

    public static IEnumerable<object[]> SecretKeyRails() =>
    [
        [DestinationType.BitcoinAddress, "bitcoin", "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh", "bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4", false],
        [DestinationType.UsdtPolygon, "pol", "0x278297528c32eCa01295B0d2135984d3442ec963", "0x1111111111111111111111111111111111111111", true],
        [DestinationType.UsdcPolygon, "pol", "0xAb5801a7D398351b8bE11C439e05C5B3259aeC9B", "0x2222222222222222222222222222222222222222", true],
        [DestinationType.UsdtEthereum, "eth", "0xBF5550253939187432Ef4dccE8089765d3DCb47f", "0x3333333333333333333333333333333333333333", true],
        [DestinationType.UsdcEthereum, "eth", "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48", "0x4444444444444444444444444444444444444444", true],
        [DestinationType.UsdtBsc, "bsc", "0x55d398326f99059fF775485246999027B3197955", "0x5555555555555555555555555555555555555555", true],
        [DestinationType.UsdcBsc, "bsc", "0x8AC76a51cc950d9822D68b83fE1Ad97B32Cd580d", "0x6666666666666666666666666666666666666666", true],
        [DestinationType.UsdtTron, "tron", "T9yD14Nj9j7xAB4dbGeiX9h8unkKHxuWwb", "TXyZ1111111111111111111111111111111", false],
        [DestinationType.UsdtTon, "ton", "EQDtFpEwcFAEcRe5mLVh2N6C0y-_tur3BYOM93pEZPw390by", "EQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAM9c", false],
        [DestinationType.UsdtSolana, "sol", "Es9vMFrzaCERmJfrF4H2FYD4KCoNkY11McCe8BenwNYB", "11111111111111111111111111111111", false],
        [DestinationType.UsdcSolana, "sol", "7EqQdEULxWcraVx3mXKFjc84LhCkMGZCkRuDofycyinf", "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA", false],
    ];

    private static string SecretKeyUri(string scheme, string recipient, string encrypted, string secret) =>
        $"{scheme}:{recipient}?branta_id={Uri.EscapeDataString(encrypted)}&branta_secret={Uri.EscapeDataString(secret)}";

    [LivePostTheory]
    [MemberData(nameof(SecretKeyRails))]
    public async Task Staging_SecretKeyRoundTrip(
        DestinationType type,
        string scheme,
        string address,
        string swapped,
        bool caseFold)
    {
        var added = await _service.AddPaymentAsync(new PaymentBuilder()
            .AddDestination(address, type)
            .SetTtl(120)
            .Build());

        var encrypted = added.Payment.Destinations[0].Value;
        Assert.Equal(type, added.Payment.Destinations[0].Type);

        var match = await _service.VerifyAsync(SecretKeyUri(scheme, address, encrypted, added.Secret));
        Assert.Equal(VerifyStatus.Match, match.Status);
        Assert.Equal(type, match.MatchedDestinations![0].Type);
        if (caseFold)
        {
            Assert.Equal(address, match.MatchedDestinations[0].Value, ignoreCase: true);
            Assert.Equal(
                VerifyStatus.Match,
                (await _service.VerifyAsync(SecretKeyUri(scheme, address.ToLowerInvariant(), encrypted, added.Secret))).Status);
        }
        else
        {
            Assert.Equal(address, match.MatchedDestinations[0].Value);
        }

        Assert.Equal(
            VerifyStatus.Tampered,
            (await _service.VerifyAsync(SecretKeyUri(scheme, swapped, encrypted, added.Secret))).Status);
        Assert.Equal(VerifyStatus.Unknown, (await _service.VerifyAsync(address)).Status);
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
