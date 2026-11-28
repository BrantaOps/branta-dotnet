using Branta;
using Microsoft.Extensions.Options;
using Moq;

namespace Branta.Tests.Services;

public class BrantaServiceTests
{
    private readonly Mock<IBrantaClient> _clientMock;
    private readonly Mock<IAesEncryption> _aesEncryptionMock;
    private readonly Mock<ISecretGenerator> _secretGeneratorMock;
    private readonly BrantaApiOptions _defaultOptions;
    private readonly BrantaService _service;

    private const string BitcoinAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
    private const string EncryptedBitcoinAddress = "encrypted-bitcoin-address";
    private const string Secret = "test-secret";

    private const string Bolt11Invoice = "lnbc100n1ptest";
    private const string EncryptedBolt11 = "encrypted-bolt11-value";
    private static readonly string Bolt11Hash = Bolt11Invoice.ToNormalizedHash();

    private const string UsdtPolygonAddress = "0x278297528c32eCa01295B0d2135984d3442ec963";
    private const string UsdcPolygonAddress = "0xAb5801a7D398351b8bE11C439e05C5B3259aeC9B";
    private const string SiblingUsdtPolygonAddress = "0x2222222222222222222222222222222222222222";
    private const string UsdtEthereumAddress = "0xBF5550253939187432Ef4dccE8089765d3DCb47f";
    private const string EncryptedUsdtEthereumAddress = "encrypted-usdt-ethereum-address";
    private const string UsdcEthereumAddress = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
    private const string EncryptedUsdcEthereumAddress = "encrypted-usdc-ethereum-address";
    private const string UsdtBscAddress = "0x55d398326f99059fF775485246999027B3197955";
    private const string EncryptedUsdtBscAddress = "encrypted-usdt-bsc-address";
    private const string UsdcBscAddress = "0x8AC76a51cc950d9822D68b83fE1Ad97B32Cd580d";
    private const string EncryptedUsdcBscAddress = "encrypted-usdc-bsc-address";
    private const string UsdtTronAddress = "T9yD14Nj9j7xAB4dbGeiX9h8unkKHxuWwb";
    private const string EncryptedUsdtTronAddress = "encrypted-usdt-tron-address";
    private const string UsdtTonAddress = "EQDtFpEwcFAEcRe5mLVh2N6C0y-_tur3BYOM93pEZPw390by";
    private const string EncryptedUsdtTonAddress = "encrypted-usdt-ton-address";
    private const string UsdtSolanaAddress = "Es9vMFrzaCERmJfrF4H2FYD4KCoNkY11McCe8BenwNYB";
    private const string EncryptedUsdtSolanaAddress = "encrypted-usdt-solana-address";
    private const string UsdcSolanaAddress = "7EqQdEULxWcraVx3mXKFjc84LhCkMGZCkRuDofycyinf";
    private const string EncryptedUsdcSolanaAddress = "encrypted-usdc-solana-address";
    private const string EncryptedUsdtPolygonAddress = "encrypted-usdt-polygon-address";
    private const string EncryptedUsdcPolygonAddress = "encrypted-usdc-polygon-address";
    private const string EncryptedSiblingUsdtPolygonAddress = "encrypted-sibling-usdt-polygon-address";
    private const string EncryptedUsdcAtUsdtAddress = "encrypted-usdc-at-usdt-address";

    private static Payment EncryptedBitcoinPayment() => new()
    {
        Destinations = [new Destination { Value = EncryptedBitcoinAddress, Type = DestinationType.BitcoinAddress, Id = "zk1" }]
    };

    private static Payment PlainBitcoinPayment() => new()
    {
        Destinations = [new Destination { Value = BitcoinAddress, Type = DestinationType.BitcoinAddress, Id = "zk1", IsEncrypted = false }]
    };

    private static Payment EncryptedBolt11Payment() => new()
    {
        Destinations = [new Destination { Value = EncryptedBolt11, Type = DestinationType.Bolt11, Id = "zk-bolt11" }]
    };

    private static Payment PlainBolt11Payment() => new()
    {
        Destinations = [new Destination { Value = Bolt11Invoice, Type = DestinationType.Bolt11, Id = "zk-bolt11", IsEncrypted = false }]
    };

    private static string BrantaBitcoinLookup(string secret = Secret) =>
        $"bitcoin:{BitcoinAddress}?branta_id={EncryptedBitcoinAddress}&branta_secret={secret}";

    public BrantaServiceTests()
    {
        _clientMock = new Mock<IBrantaClient>();
        _aesEncryptionMock = new Mock<IAesEncryption>();
        _secretGeneratorMock = new Mock<ISecretGenerator>();
        _secretGeneratorMock.Setup(g => g.Generate()).Returns(Secret);
        _secretGeneratorMock.Setup(g => g.DeterministicNonce).Returns(false);
        _defaultOptions = new BrantaApiOptions
        {
            Environment = BrantaEnvironment.Localhost,
            ApiKey = "test-api-key"
        };
        _service = new BrantaService(_clientMock.Object, _aesEncryptionMock.Object, Options.Create(_defaultOptions), _secretGeneratorMock.Object);

        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBitcoinAddress, Secret)).Returns(BitcoinAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBolt11, Bolt11Hash)).Returns(Bolt11Invoice);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdtPolygonAddress, Secret)).Returns(UsdtPolygonAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdcPolygonAddress, Secret)).Returns(UsdcPolygonAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedSiblingUsdtPolygonAddress, Secret)).Returns(SiblingUsdtPolygonAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdcAtUsdtAddress, Secret)).Returns(UsdtPolygonAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdtEthereumAddress, Secret)).Returns(UsdtEthereumAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdcEthereumAddress, Secret)).Returns(UsdcEthereumAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdtBscAddress, Secret)).Returns(UsdtBscAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdcBscAddress, Secret)).Returns(UsdcBscAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdtTronAddress, Secret)).Returns(UsdtTronAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdtTonAddress, Secret)).Returns(UsdtTonAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdtSolanaAddress, Secret)).Returns(UsdtSolanaAddress);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedUsdcSolanaAddress, Secret)).Returns(UsdcSolanaAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdtTronAddress, Secret, false)).Returns(EncryptedUsdtTronAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(BitcoinAddress, Secret, false)).Returns(EncryptedBitcoinAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(Bolt11Invoice.ToLowerInvariant(), Bolt11Hash, true)).Returns(EncryptedBolt11);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdtPolygonAddress, Secret, false)).Returns(EncryptedUsdtPolygonAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdcPolygonAddress, Secret, false)).Returns(EncryptedUsdcPolygonAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdtEthereumAddress, Secret, false)).Returns(EncryptedUsdtEthereumAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdcEthereumAddress, Secret, false)).Returns(EncryptedUsdcEthereumAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdtBscAddress, Secret, false)).Returns(EncryptedUsdtBscAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdcBscAddress, Secret, false)).Returns(EncryptedUsdcBscAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdtTonAddress, Secret, false)).Returns(EncryptedUsdtTonAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdtSolanaAddress, Secret, false)).Returns(EncryptedUsdtSolanaAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(UsdcSolanaAddress, Secret, false)).Returns(EncryptedUsdcSolanaAddress);
    }

    private static void ExpectMatch(VerifyResult result)
    {
        Assert.Equal(VerifyStatus.Match, result.Status);
        Assert.NotNull(result.Payments);
        Assert.NotEmpty(result.Payments);
    }

    private static void ExpectUnknown(VerifyResult result)
    {
        Assert.Equal(VerifyStatus.Unknown, result.Status);
        Assert.Null(result.Payments);
    }

    private static void ExpectTampered(VerifyResult result)
    {
        Assert.Equal(VerifyStatus.Tampered, result.Status);
        Assert.Null(result.Payments);
    }

    private static void ExpectIncomplete(VerifyResult result)
    {
        Assert.Equal(VerifyStatus.Incomplete, result.Status);
        Assert.Null(result.Payments);
    }

    [Fact]
    public async Task Verify_BitcoinBrantaUri_UsesBrantaParams()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        var result = await _service.VerifyAsync(BrantaBitcoinLookup());

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        ExpectMatch(result);
        Assert.Equal(BitcoinAddress, result.Payments![0].Destinations[0].Value);
    }

    [Fact]
    public async Task Verify_BareBitcoinUri_ReturnsUnknown()
    {
        var result = await _service.VerifyAsync($"bitcoin:{BitcoinAddress}");

        ExpectUnknown(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_LightningBolt11Uri_UsesEncryptedInvoiceLookup()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBolt11Payment()]);

        var result = await _service.VerifyAsync($"lightning:{Bolt11Invoice}");

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        ExpectMatch(result);
        Assert.Equal(Bolt11Invoice, result.Payments![0].Destinations[0].Value);
    }

    [Fact]
    public async Task Verify_UppercaseLightningBolt11Uri_UsesEncryptedInvoiceLookup()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBolt11Payment()]);

        var result = await _service.VerifyAsync($"lightning:{Bolt11Invoice.ToUpperInvariant()}");

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        ExpectMatch(result);
        Assert.Equal(Bolt11Invoice, result.Payments![0].Destinations[0].Value);
    }

    [Fact]
    public async Task Verify_Bolt11DecryptingToADifferentInvoice_IsTampered()
    {
        _aesEncryptionMock.Setup(e => e.Decrypt("encrypted-attacker-invoice", Bolt11Hash)).Returns("lnbc1attacker");
        var payment = new Payment
        {
            Destinations =
            [
                new Destination { Value = "copied-lookup", Type = DestinationType.BitcoinAddress, Id = "btc" },
                new Destination { Value = "encrypted-attacker-invoice", Type = DestinationType.Bolt11, Id = "bolt" }
            ]
        };
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await _service.VerifyAsync($"lightning:{Bolt11Invoice}");

        ExpectTampered(result);
        Assert.Equal($"http://localhost:3000/v2/verify/{Bolt11Invoice}", result.VerifyUrl);
        Assert.DoesNotContain("#", result.VerifyUrl);
    }

    [Fact]
    public async Task Verify_LightningBolt11Uri_LeavesUnrelatedEncryptedBitcoinDestinationEncrypted()
    {
        var payment = new Payment
        {
            Destinations =
            [
                new Destination { Value = EncryptedBolt11, Type = DestinationType.Bolt11, Id = "zk-bolt11" },
                new Destination { Value = EncryptedBitcoinAddress, Type = DestinationType.BitcoinAddress, Id = "zk-btc" }
            ]
        };
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await _service.VerifyAsync($"lightning:{Bolt11Invoice}");

        ExpectMatch(result);
        Assert.Equal(EncryptedBitcoinAddress, result.Payments![0].Destinations[1].Value);
        Assert.True(result.Payments[0].Destinations[1].IsEncrypted);
    }

    [Fact]
    public async Task Verify_CombinedBrantaQr_DecryptsBothAddressAndInvoice()
    {
        var payment = new Payment
        {
            Destinations =
            [
                new Destination { Value = EncryptedBitcoinAddress, Type = DestinationType.BitcoinAddress, Id = "zk-btc" },
                new Destination { Value = EncryptedBolt11, Type = DestinationType.Bolt11, Id = "zk-bolt11" }
            ]
        };
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var qr = $"bitcoin:{BitcoinAddress}?lightning={Bolt11Invoice}&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}";
        var result = await _service.VerifyAsync(qr);

        ExpectMatch(result);
        Assert.Equal(BitcoinAddress, result.Payments![0].Destinations[0].Value);
        Assert.Equal(Bolt11Invoice, result.Payments[0].Destinations[1].Value);
        Assert.Equal(2, result.MatchedDestinations!.Count);
        Assert.Equal(DestinationType.BitcoinAddress, result.MatchedDestinations[0].Type);
        Assert.Equal(BitcoinAddress, result.MatchedDestinations[0].Value);
        Assert.Equal(DestinationType.Bolt11, result.MatchedDestinations[1].Type);
        Assert.Equal(Bolt11Invoice, result.MatchedDestinations[1].Value);
    }

    [Fact]
    public async Task Verify_BitcoinBrantaUri_SwappedAddress_IsTampered()
    {
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBitcoinAddress, Secret)).Returns("bc1qother");
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        var result = await _service.VerifyAsync(BrantaBitcoinLookup());

        ExpectTampered(result);
        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}", result.VerifyUrl);
    }

    [Fact]
    public async Task Verify_BitcoinBrantaUri_MatchingAddress_IsMatch()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        var result = await _service.VerifyAsync(BrantaBitcoinLookup());

        ExpectMatch(result);
        Assert.Single(result.MatchedDestinations!);
        Assert.Equal(DestinationType.BitcoinAddress, result.MatchedDestinations[0].Type);
        Assert.Equal(BitcoinAddress, result.MatchedDestinations[0].Value);
    }

    [Fact]
    public async Task Verify_BitcoinBrantaUri_UppercaseBech32Qr_MatchesLowercaseRegistered()
    {
        const string lower = "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh";
        const string upper = "BC1QXY2KGDYGJRSQTZQ2N0YRF2493P83KKFJHX0WLH";
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBitcoinAddress, Secret)).Returns(lower);
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        var qr = $"bitcoin:{upper}?branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}";
        ExpectMatch(await _service.VerifyAsync(qr));
    }

    [Fact]
    public async Task Verify_BitcoinBrantaUri_Base58CaseMismatch_IsTampered()
    {
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBitcoinAddress, Secret)).Returns(BitcoinAddress.ToLowerInvariant());
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        ExpectTampered(await _service.VerifyAsync(BrantaBitcoinLookup()));
    }

    [Fact]
    public async Task Verify_CombinedBrantaQr_SwappedAddress_IsTampered()
    {
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBitcoinAddress, Secret)).Returns("bc1qother");
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        var qr = $"bitcoin:{BitcoinAddress}?lightning={Bolt11Invoice}&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}";
        ExpectTampered(await _service.VerifyAsync(qr));
    }

    [Fact]
    public async Task Verify_SwappedAddressAndWrongSecret_ReturnsUnknown()
    {
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBitcoinAddress, "wrong-key")).Throws(new Exception("auth tag mismatch"));
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        ExpectUnknown(await _service.VerifyAsync(BrantaBitcoinLookup("wrong-key")));
    }

    [Fact]
    public async Task Verify_AttackerLightningLeg_ReturnsUnknown()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        var qr = $"bitcoin:{BitcoinAddress}?lightning=lnbc1attacker&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}";
        ExpectUnknown(await _service.VerifyAsync(qr));
    }

    [Fact]
    public async Task Verify_AttackerArkLeg_ReturnsUnknown()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        var qr = $"bitcoin:{BitcoinAddress}?ark=ark1attacker&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}";
        ExpectUnknown(await _service.VerifyAsync(qr));
    }

    [Fact]
    public async Task Verify_Bip321BcSwap_IsTampered()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        ExpectTampered(await _service.VerifyAsync($"bitcoin:?bc=bc1qother&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}"));
    }

    [Fact]
    public async Task Verify_Bip321BcMatch_IsMatch()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        ExpectMatch(await _service.VerifyAsync($"bitcoin:?bc={BitcoinAddress}&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}"));
    }

    [Fact]
    public async Task Verify_PathAndBcDisagree_IsTamperedBeforeLookup()
    {
        var result = await _service.VerifyAsync($"bitcoin:{BitcoinAddress}?bc=bc1qother&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}");

        ExpectTampered(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_PayInstruction_ReturnsUnknown()
    {
        var result = await _service.VerifyAsync($"bitcoin:{BitcoinAddress}?pay=bitcoin:bc1qother&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}");

        ExpectUnknown(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_LnoInstruction_ReturnsUnknown()
    {
        var result = await _service.VerifyAsync($"bitcoin:{BitcoinAddress}?lno=lno1qqexample&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}");

        ExpectUnknown(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_NoPayableInstruction_ReturnsUnknown()
    {
        var result = await _service.VerifyAsync($"bitcoin:?branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}");

        ExpectUnknown(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_ClientReturnsEmpty_IsUnknown()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.VerifyAsync(BrantaBitcoinLookup());

        ExpectUnknown(result);
        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}", result.VerifyUrl);
    }

    [Fact]
    public async Task Verify_ClientLookupDoesNotFinish_IsIncomplete()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IncompleteError("InternalServerError"));

        var result = await _service.VerifyAsync(BrantaBitcoinLookup());

        ExpectIncomplete(result);
        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}", result.VerifyUrl);
    }

    [Fact]
    public async Task Verify_ForwardsOptions_ToClient()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(It.IsAny<string>(), _defaultOptions, It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        await _service.VerifyAsync(BrantaBitcoinLookup(), _defaultOptions);

        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), _defaultOptions, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Verify_ForwardsCancellationToken_ToClient()
    {
        using var cts = new CancellationTokenSource();
        _clientMock
            .Setup(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), cts.Token))
            .ReturnsAsync([]);

        await _service.VerifyAsync(BrantaBitcoinLookup(), cancellationToken: cts.Token);

        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task Verify_WrongKey_ReturnsUnknown()
    {
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBitcoinAddress, "wrong-key")).Throws(new Exception("auth tag mismatch"));
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        ExpectUnknown(await _service.VerifyAsync(BrantaBitcoinLookup("wrong-key")));
    }

    [Fact]
    public async Task Verify_UnencryptedDestination_ReturnsUnknown()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBitcoinPayment()]);

        ExpectUnknown(await _service.VerifyAsync(BrantaBitcoinLookup()));
        _aesEncryptionMock.Verify(e => e.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Verify_PastedBolt11_DecryptsUsingHash()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBolt11Payment()]);

        var result = await _service.VerifyAsync(Bolt11Invoice);

        ExpectMatch(result);
        Assert.Equal(Bolt11Invoice, result.Payments![0].Destinations[0].Value);
        _aesEncryptionMock.Verify(e => e.Decrypt(EncryptedBolt11, Bolt11Hash), Times.Once);
    }

    [Fact]
    public async Task Verify_UnencryptedBolt11_ReturnsUnknown()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBolt11Payment()]);

        var result = await _service.VerifyAsync(Bolt11Invoice);

        ExpectUnknown(result);
        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBolt11}", result.VerifyUrl);
        _aesEncryptionMock.Verify(e => e.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Verify_PastedBolt11_DecryptFails_ReturnsUnknown()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBolt11Payment()]);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBolt11, Bolt11Hash)).Throws(new Exception("auth tag mismatch"));

        ExpectUnknown(await _service.VerifyAsync(Bolt11Invoice));
    }

    [Fact]
    public async Task Verify_BitcoinBrantaUri_SetsVerifyUrlWithKeyFragment()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBitcoinPayment()]);

        var result = await _service.VerifyAsync(BrantaBitcoinLookup());

        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}#k-zk1={Secret}", result.VerifyUrl);
    }

    [Fact]
    public async Task Verify_Bolt11_SetsVerifyUrlWithKeyFragment()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBolt11Payment()]);

        var result = await _service.VerifyAsync(Bolt11Invoice);

        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBolt11}#k-zk-bolt11={Bolt11Hash}", result.VerifyUrl);
    }

    [Fact]
    public async Task Verify_Bolt11NotFound_DoesNotLookUpRawInvoice()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _clientMock
            .Setup(c => c.GetPaymentsAsync(Bolt11Invoice, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBolt11Payment()]);

        var result = await _service.VerifyAsync(Bolt11Invoice);

        ExpectUnknown(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.GetPaymentsAsync(Bolt11Invoice, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_BareBitcoinAddress_ReturnsUnknown()
    {
        ExpectUnknown(await _service.VerifyAsync(BitcoinAddress));
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private const string Deposit = "0xAbCdEf0123456789aBcDeF0123456789AbCdEf01";
    private const string SwappedDeposit = "0x1111111111111111111111111111111111111111";
    private const string SiblingDeposit = "0x2222222222222222222222222222222222222222";

    public static IEnumerable<object[]> EvmRails() =>
        DestinationTypes.EvmTokenRails.Select(r => new object[] { r.Type });

    /// <summary>ERC-681 transfer URI for a rail, with the branta params.</summary>
    private static string TransferUri(DestinationType type, string address, string encrypted, string secret = Secret)
    {
        var rail = DestinationTypes.EvmTokenRails.Single(r => r.Type == type);
        return $"ethereum:{rail.Contract}@{rail.ChainId}/transfer?address={address}&uint256=50000000" +
               $"&branta_id={Uri.EscapeDataString(encrypted)}&branta_secret={Uri.EscapeDataString(secret)}";
    }

    /// <summary>
    /// Serves one payment whose destinations decrypt to the given addresses with <see cref="Secret"/>.
    /// Returns each destination's ciphertext, which is also the lookup the client is called with.
    /// </summary>
    private string[] ServePayment(params (DestinationType Type, string Address)[] destinations)
    {
        var encrypted = new string[destinations.Length];
        var list = new List<Destination>();
        for (var i = 0; i < destinations.Length; i++)
        {
            encrypted[i] = $"encrypted-{destinations[i].Type}-{i}";
            _aesEncryptionMock.Setup(e => e.Decrypt(encrypted[i], Secret)).Returns(destinations[i].Address);
            list.Add(new Destination { Value = encrypted[i], Type = destinations[i].Type, Id = $"zk-{i}" });
        }

        foreach (var lookup in encrypted)
        {
            _clientMock
                .Setup(c => c.GetPaymentsAsync(lookup, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([new Payment { Destinations = list }]);
        }
        return encrypted;
    }

    private void ExpectNoLookup() =>
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);

    [Theory]
    [MemberData(nameof(EvmRails))]
    public async Task Verify_Erc681_EveryRailMatchesAndSetsMatchedDestinations(DestinationType type)
    {
        var encrypted = ServePayment((type, Deposit));

        var result = await _service.VerifyAsync(TransferUri(type, Deposit, encrypted[0]));

        _clientMock.Verify(c => c.GetPaymentsAsync(encrypted[0], It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        ExpectMatch(result);
        Assert.Equal(Deposit, result.Payments![0].Destinations[0].Value);
        Assert.Equal(type, result.Payments[0].Destinations[0].Type);
        Assert.Single(result.MatchedDestinations!);
        Assert.Equal(type, result.MatchedDestinations![0].Type);
        Assert.Equal(Deposit, result.MatchedDestinations[0].Value);
        Assert.Contains("#k-zk-0=", result.VerifyUrl);
    }

    [Theory]
    [MemberData(nameof(EvmRails))]
    public async Task Verify_Erc681_LowercaseDepositMatches(DestinationType type)
    {
        var encrypted = ServePayment((type, Deposit));

        var result = await _service.VerifyAsync(TransferUri(type, Deposit.ToLowerInvariant(), encrypted[0]));

        ExpectMatch(result);
        Assert.Equal(Deposit, result.MatchedDestinations![0].Value);
    }

    [Fact]
    public async Task Verify_Erc681_UpperCase0xDepositPrefixMatches()
    {
        var encrypted = ServePayment((DestinationType.UsdtBase, Deposit));

        ExpectMatch(await _service.VerifyAsync(TransferUri(DestinationType.UsdtBase, "0X" + Deposit[2..], encrypted[0])));
    }

    [Theory]
    [MemberData(nameof(EvmRails))]
    public async Task Verify_Erc681_SwappedDepositIsTampered(DestinationType type)
    {
        var encrypted = ServePayment((type, Deposit));

        var result = await _service.VerifyAsync(TransferUri(type, SwappedDeposit, encrypted[0]));

        ExpectTampered(result);
        Assert.Null(result.MatchedDestinations);
        Assert.DoesNotContain('#', result.VerifyUrl);
    }

    [Theory]
    [MemberData(nameof(EvmRails))]
    public async Task Verify_Erc681_ContractOnTheWrongChain_IsUnknownWithoutLookup(DestinationType type)
    {
        var rail = DestinationTypes.EvmTokenRails.Single(r => r.Type == type);
        var wrongChain = rail.ChainId == 1 ? 137 : 1;

        var result = await _service.VerifyAsync(
            $"ethereum:{rail.Contract}@{wrongChain}/transfer?address={Deposit}&branta_id=abc&branta_secret={Secret}");

        ExpectUnknown(result);
        ExpectNoLookup();
    }

    [Theory]
    [MemberData(nameof(EvmRails))]
    public async Task Verify_Erc681_WithoutBrantaParams_IsUnknownWithoutLookup(DestinationType type)
    {
        var rail = DestinationTypes.EvmTokenRails.Single(r => r.Type == type);

        var result = await _service.VerifyAsync($"ethereum:{rail.Contract}@{rail.ChainId}/transfer?address={Deposit}&uint256=1");

        ExpectUnknown(result);
        Assert.DoesNotContain('#', result.VerifyUrl);
        ExpectNoLookup();
    }

    [Fact]
    public async Task Verify_Erc681_DualTokenSameDeposit_BindsOnlyTheScannedToken()
    {
        var encrypted = ServePayment((DestinationType.UsdtBase, Deposit), (DestinationType.UsdcBase, Deposit));

        var result = await _service.VerifyAsync(TransferUri(DestinationType.UsdtBase, Deposit, encrypted[0]));

        ExpectMatch(result);
        Assert.Single(result.MatchedDestinations!);
        Assert.Equal(DestinationType.UsdtBase, result.MatchedDestinations![0].Type);
        Assert.False(result.Payments![0].Destinations[0].IsEncrypted);
        Assert.True(result.Payments[0].Destinations[1].IsEncrypted);
        Assert.Equal(encrypted[1], result.Payments[0].Destinations[1].Value);
    }

    [Fact]
    public async Task Verify_Erc681_UsdcScanAgainstUsdtOnlyPayment_IsUnknown()
    {
        var encrypted = ServePayment((DestinationType.UsdtBase, Deposit));

        var result = await _service.VerifyAsync(TransferUri(DestinationType.UsdcBase, Deposit, encrypted[0]));

        ExpectUnknown(result);
        Assert.Null(result.MatchedDestinations);
    }

    [Fact]
    public async Task Verify_Erc681_EthereumScanAgainstPolygonOnlyPayment_IsUnknown()
    {
        var encrypted = ServePayment((DestinationType.UsdtPolygon, Deposit));

        ExpectUnknown(await _service.VerifyAsync(TransferUri(DestinationType.UsdtEthereum, Deposit, encrypted[0])));
    }

    [Fact]
    public async Task Verify_Erc681_SameTypeSiblingWithAnotherAddress_StaysEncrypted()
    {
        var encrypted = ServePayment((DestinationType.UsdtArbitrum, SiblingDeposit), (DestinationType.UsdtArbitrum, Deposit));

        var result = await _service.VerifyAsync(TransferUri(DestinationType.UsdtArbitrum, Deposit, encrypted[1]));

        ExpectMatch(result);
        Assert.True(result.Payments![0].Destinations[0].IsEncrypted);
        Assert.Equal(encrypted[0], result.Payments[0].Destinations[0].Value);
        Assert.False(result.Payments[0].Destinations[1].IsEncrypted);
        Assert.Single(result.MatchedDestinations!);
    }

    [Fact]
    public async Task Verify_Erc681_SwappedDepositIsStillTamperedWithASameTypeSibling()
    {
        var encrypted = ServePayment((DestinationType.UsdtArbitrum, SiblingDeposit), (DestinationType.UsdtArbitrum, Deposit));

        ExpectTampered(await _service.VerifyAsync(TransferUri(DestinationType.UsdtArbitrum, SwappedDeposit, encrypted[1])));
    }

    [Fact]
    public async Task Verify_Erc681_LeavesABitcoinSiblingEncrypted()
    {
        var encrypted = ServePayment((DestinationType.BitcoinAddress, BitcoinAddress), (DestinationType.UsdtPolygon, Deposit));

        var result = await _service.VerifyAsync(TransferUri(DestinationType.UsdtPolygon, Deposit, encrypted[1]));

        ExpectMatch(result);
        Assert.True(result.Payments![0].Destinations[0].IsEncrypted);
        Assert.Equal(encrypted[0], result.Payments[0].Destinations[0].Value);
        Assert.False(result.Payments[0].Destinations[1].IsEncrypted);
        Assert.Equal(Deposit, result.Payments[0].Destinations[1].Value);
        Assert.Single(result.MatchedDestinations!);
        Assert.Equal(DestinationType.UsdtPolygon, result.MatchedDestinations![0].Type);
    }

    [Fact]
    public async Task Verify_BitcoinUri_LeavesAnEvmSiblingEncrypted()
    {
        var encrypted = ServePayment((DestinationType.BitcoinAddress, BitcoinAddress), (DestinationType.UsdtPolygon, Deposit));

        var result = await _service.VerifyAsync($"bitcoin:{BitcoinAddress}?branta_id={encrypted[0]}&branta_secret={Secret}");

        ExpectMatch(result);
        Assert.False(result.Payments![0].Destinations[0].IsEncrypted);
        Assert.True(result.Payments[0].Destinations[1].IsEncrypted);
        Assert.Equal(encrypted[1], result.Payments[0].Destinations[1].Value);
        Assert.Single(result.MatchedDestinations!);
        Assert.Equal(DestinationType.BitcoinAddress, result.MatchedDestinations![0].Type);
    }

    [Fact]
    public async Task Verify_Erc681_WrongSecret_IsUnknown()
    {
        var encrypted = ServePayment((DestinationType.UsdcArbitrum, Deposit));
        _aesEncryptionMock.Setup(e => e.Decrypt(encrypted[0], "wrong-key")).Throws(new Exception("auth tag mismatch"));

        ExpectUnknown(await _service.VerifyAsync(TransferUri(DestinationType.UsdcArbitrum, Deposit, encrypted[0], "wrong-key")));
    }

    [Fact]
    public async Task Verify_Erc681_DecryptedMetadata_LiftsDescription()
    {
        const string encryptedDek = "encrypted-dek-blob";
        const string decryptedDek = "dek";
        const string encryptedMetadata = "encrypted-meta";
        const string decryptedMetadata = "{\"description\":\"USDT invoice\"}";
        _aesEncryptionMock.Setup(e => e.Decrypt(encryptedDek, Secret)).Returns(decryptedDek);
        _aesEncryptionMock.Setup(e => e.Decrypt(encryptedMetadata, decryptedDek)).Returns(decryptedMetadata);
        _aesEncryptionMock.Setup(e => e.Decrypt("encrypted-evm", Secret)).Returns(Deposit);
        _clientMock
            .Setup(c => c.GetPaymentsAsync("encrypted-evm", It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Payment
            {
                Metadata = encryptedMetadata,
                Destinations = [new Destination { Value = "encrypted-evm", Type = DestinationType.UsdtBase, Id = "zk1", EncryptedDek = encryptedDek }],
            }]);

        var result = await _service.VerifyAsync(TransferUri(DestinationType.UsdtBase, Deposit, "encrypted-evm"));

        ExpectMatch(result);
        Assert.Equal("USDT invoice", result.Payments![0].Description);
    }

    public static IEnumerable<object[]> UnsupportedErc681Forms()
    {
        var contract = DestinationTypes.EvmTokenRails.Single(r => r.Type == DestinationType.UsdcBase).Contract;
        var tail = $"address={Deposit}&branta_id=abc&branta_secret={Secret}";
        string[] forms =
        [
            $"ethereum:{contract}@8453/approve?{tail}",
            $"ethereum:{contract}@8453/transferFrom?{tail}",
            $"ethereum://{contract}@8453/transfer?{tail}",
            $"ethereum:{contract}/transfer?{tail}",
            $"ethereum:{contract}@0x2105/transfer?{tail}",
            "ethereum://[bad/transfer",
            "bitcoin://[bad",
            $"ethereum:{contract}@{new string('9', 5000)}/transfer?{tail}",
            $"ethereum:{contract}@8453/transfer#x?{tail}",
        ];
        return forms.Select(f => new object[] { f });
    }

    [Theory]
    [MemberData(nameof(UnsupportedErc681Forms))]
    public async Task Verify_UnsupportedErc681Forms_AreUnknownWithoutLookup(string qr)
    {
        ExpectUnknown(await _service.VerifyAsync(qr));
        ExpectNoLookup();
    }

    [Theory]
    [InlineData("pol", UsdtPolygonAddress)]
    [InlineData("eth", UsdtEthereumAddress)]
    [InlineData("bsc", UsdtBscAddress)]
    [InlineData("tron", UsdtTronAddress)]
    [InlineData("ton", UsdtTonAddress)]
    [InlineData("sol", UsdcSolanaAddress)]
    public async Task Verify_RemovedChainSchemes_AreUnknownWithoutLookup(string scheme, string address)
    {
        var result = await _service.VerifyAsync($"{scheme}:{address}?value=50&branta_id=abc&branta_secret={Secret}");

        ExpectUnknown(result);
        Assert.DoesNotContain('#', result.VerifyUrl);
        ExpectNoLookup();
    }

    [Fact]
    public async Task Verify_BareChainAddresses_AreUnknown()
    {
        ExpectUnknown(await _service.VerifyAsync(UsdtPolygonAddress));
        ExpectUnknown(await _service.VerifyAsync(UsdtEthereumAddress));
        ExpectUnknown(await _service.VerifyAsync(UsdtTronAddress));
        ExpectUnknown(await _service.VerifyAsync(UsdtTonAddress));
        ExpectUnknown(await _service.VerifyAsync(UsdcSolanaAddress));
        ExpectNoLookup();
    }

    [Theory]
    [MemberData(nameof(EvmRails))]
    public async Task AddPayment_EveryEvmRail_EncryptsWithSecret(DestinationType type)
    {
        const string encryptedDeposit = "encrypted-deposit";
        _aesEncryptionMock.Setup(e => e.Encrypt(Deposit, Secret, false)).Returns(encryptedDeposit);
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder().AddDestination(Deposit, type).Build();
        var result = await _service.AddPaymentAsync(payment);

        Assert.Equal(Deposit, payment.Destinations[0].Value);
        Assert.Equal(type, posted!.Destinations[0].Type);
        Assert.Equal(encryptedDeposit, posted.Destinations[0].Value);
        Assert.Equal(Secret, result.Secret);
    }

    [Theory]
    [InlineData(UsdtPolygonAddress, DestinationType.UsdtPolygon, EncryptedUsdtPolygonAddress)]
    [InlineData(UsdcPolygonAddress, DestinationType.UsdcPolygon, EncryptedUsdcPolygonAddress)]
    [InlineData(UsdtEthereumAddress, DestinationType.UsdtEthereum, EncryptedUsdtEthereumAddress)]
    [InlineData(UsdcEthereumAddress, DestinationType.UsdcEthereum, EncryptedUsdcEthereumAddress)]
    [InlineData(UsdtBscAddress, DestinationType.UsdtBsc, EncryptedUsdtBscAddress)]
    [InlineData(UsdcBscAddress, DestinationType.UsdcBsc, EncryptedUsdcBscAddress)]
    [InlineData(UsdtTronAddress, DestinationType.UsdtTron, EncryptedUsdtTronAddress)]
    [InlineData(UsdtTonAddress, DestinationType.UsdtTon, EncryptedUsdtTonAddress)]
    [InlineData(UsdtSolanaAddress, DestinationType.UsdtSolana, EncryptedUsdtSolanaAddress)]
    [InlineData(UsdcSolanaAddress, DestinationType.UsdcSolana, EncryptedUsdcSolanaAddress)]
    public async Task AddPayment_SecretKeyRails_EncryptWithSecret(string address, DestinationType type, string encrypted)
    {
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder().AddDestination(address, type).Build();
        var result = await _service.AddPaymentAsync(payment);

        Assert.Equal(address, payment.Destinations[0].Value);
        Assert.Equal(type, posted!.Destinations[0].Type);
        Assert.Equal(encrypted, posted.Destinations[0].Value);
        Assert.Equal(Secret, result.Secret);
        _aesEncryptionMock.Verify(e => e.Encrypt(address, Secret, false), Times.Once);
    }

    [Fact]
    public async Task AddPayment_UsdtPolygon_EncryptsWithSecret()
    {
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder().AddDestination(UsdtPolygonAddress, DestinationType.UsdtPolygon).Build();
        await _service.AddPaymentAsync(payment);

        Assert.Equal(UsdtPolygonAddress, payment.Destinations[0].Value);
        Assert.Equal(DestinationType.UsdtPolygon, posted!.Destinations[0].Type);
        Assert.Equal(EncryptedUsdtPolygonAddress, posted.Destinations[0].Value);
        _aesEncryptionMock.Verify(e => e.Encrypt(UsdtPolygonAddress, Secret, false), Times.Once);
    }

    [Fact]
    public async Task AddPayment_UsdtTron_EncryptsWithSecret()
    {
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder().AddDestination(UsdtTronAddress, DestinationType.UsdtTron).Build();
        await _service.AddPaymentAsync(payment);

        Assert.Equal(UsdtTronAddress, payment.Destinations[0].Value);
        Assert.Equal(DestinationType.UsdtTron, posted!.Destinations[0].Type);
        Assert.Equal(EncryptedUsdtTronAddress, posted.Destinations[0].Value);
        _aesEncryptionMock.Verify(e => e.Encrypt(UsdtTronAddress, Secret, false), Times.Once);
    }

    [Fact]
    public async Task AddPayment_UsdcPolygon_EncryptsWithSecret()
    {
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder().AddDestination(UsdcPolygonAddress, DestinationType.UsdcPolygon).Build();
        await _service.AddPaymentAsync(payment);

        Assert.Equal(UsdcPolygonAddress, payment.Destinations[0].Value);
        Assert.Equal(DestinationType.UsdcPolygon, posted!.Destinations[0].Type);
        Assert.Equal(EncryptedUsdcPolygonAddress, posted.Destinations[0].Value);
    }

    [Fact]
    public async Task AddPayment_UsdtPolygonWithoutType_CannotEncrypt()
    {
        var payment = new PaymentBuilder().AddDestination(UsdtPolygonAddress).Build();

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _service.AddPaymentAsync(payment));
        _clientMock.Verify(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_UnknownValue_ReturnsUnknown()
    {
        ExpectUnknown(await _service.VerifyAsync("not-a-destination"));
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPayment_BitcoinAddressWithoutType_EncryptsWithSecretAndSetsType()
    {
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder().AddDestination(BitcoinAddress).Build();
        await _service.AddPaymentAsync(payment);

        Assert.Equal(BitcoinAddress, payment.Destinations[0].Value);
        Assert.Equal(DestinationType.BitcoinAddress, posted!.Destinations[0].Type);
        Assert.Equal(EncryptedBitcoinAddress, posted.Destinations[0].Value);
        Assert.NotSame(payment, posted);
        _aesEncryptionMock.Verify(e => e.Encrypt(BitcoinAddress, Secret, false), Times.Once);
    }

    [Fact]
    public async Task AddPayment_Bolt11WithoutType_EncryptsWithHash()
    {
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder().AddDestination(Bolt11Invoice).Build();
        await _service.AddPaymentAsync(payment);

        Assert.Equal(Bolt11Invoice, payment.Destinations[0].Value);
        Assert.Equal(EncryptedBolt11, posted!.Destinations[0].Value);
        _aesEncryptionMock.Verify(e => e.Encrypt(Bolt11Invoice.ToLowerInvariant(), Bolt11Hash, true), Times.Once);
    }

    [Fact]
    public async Task AddPayment_ReturnsGeneratedSecret()
    {
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EncryptedBitcoinPayment());

        var payment = new PaymentBuilder().AddDestination(BitcoinAddress, DestinationType.BitcoinAddress).Build();
        var result = await _service.AddPaymentAsync(payment);

        Assert.Equal(Secret, result.Secret);
    }

    [Fact]
    public async Task AddPayment_UnsupportedType_Throws()
    {
        var payment = new PaymentBuilder().AddDestination("not-a-known-destination").Build();

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _service.AddPaymentAsync(payment));
        Assert.Equal("not-a-known-destination", payment.Destinations[0].Value);
        _clientMock.Verify(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPayment_BitcoinAddress_SetsVerifyUrlWithKeyFragment()
    {
        var posted = new Payment
        {
            Destinations = [new Destination { Value = EncryptedBitcoinAddress, Type = DestinationType.BitcoinAddress, Id = "zk1" }]
        };
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posted);

        var payment = new PaymentBuilder().AddDestination(BitcoinAddress, DestinationType.BitcoinAddress).Build();
        var result = await _service.AddPaymentAsync(payment);

        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}#k-zk1={Secret}", result.VerifyUrl);
        Assert.Equal(BitcoinAddress, payment.Destinations[0].Value);
    }

    [Fact]
    public async Task AddPayment_RetryAfterPostFailure_EncryptsTheOriginalAddress()
    {
        var posted = new List<Payment>();
        _secretGeneratorMock.SetupSequence(g => g.Generate()).Returns(Secret).Returns("retry-secret");
        _aesEncryptionMock.Setup(e => e.Encrypt(BitcoinAddress, "retry-secret", false)).Returns("encrypted-on-retry");
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted.Add(p))
            .Returns<Payment, BrantaApiOptions?, CancellationToken>((_, _, _) =>
                posted.Count == 1
                    ? Task.FromException<Payment?>(new BrantaPaymentException("500"))
                    : Task.FromResult<Payment?>(posted[^1]));

        var payment = new PaymentBuilder().AddDestination(BitcoinAddress, DestinationType.BitcoinAddress).Build();

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _service.AddPaymentAsync(payment));
        Assert.Equal(BitcoinAddress, payment.Destinations[0].Value);

        var result = await _service.AddPaymentAsync(payment);

        Assert.Equal("retry-secret", result.Secret);
        Assert.Equal(BitcoinAddress, payment.Destinations[0].Value);
        Assert.Equal(EncryptedBitcoinAddress, posted[0].Destinations[0].Value);
        Assert.Equal("encrypted-on-retry", posted[1].Destinations[0].Value);
        _aesEncryptionMock.Verify(e => e.Encrypt(BitcoinAddress, "retry-secret", false), Times.Once);
        _aesEncryptionMock.Verify(e => e.Encrypt(EncryptedBitcoinAddress, "retry-secret", false), Times.Never);
    }

    [Fact]
    public async Task AddPayment_RetryAfterPostFailure_Bolt11StillEncryptsTheInvoice()
    {
        var posted = new List<Payment>();
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted.Add(p))
            .Returns<Payment, BrantaApiOptions?, CancellationToken>((_, _, _) =>
                posted.Count == 1
                    ? Task.FromException<Payment?>(new BrantaPaymentException("500"))
                    : Task.FromResult<Payment?>(posted[^1]));

        var payment = new PaymentBuilder().AddDestination(Bolt11Invoice, DestinationType.Bolt11).Build();

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _service.AddPaymentAsync(payment));
        Assert.Equal(Bolt11Invoice, payment.Destinations[0].Value);

        await _service.AddPaymentAsync(payment);

        Assert.Equal(Bolt11Invoice, payment.Destinations[0].Value);
        Assert.Equal(EncryptedBolt11, posted[0].Destinations[0].Value);
        Assert.Equal(EncryptedBolt11, posted[1].Destinations[0].Value);
        _aesEncryptionMock.Verify(e => e.Encrypt(Bolt11Invoice.ToLowerInvariant(), Bolt11Hash, true), Times.Exactly(2));
    }

    [Fact]
    public async Task AddPayment_EmptyResponse_ThrowsAndLeavesTheCallerUnchanged()
    {
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var payment = new PaymentBuilder().AddDestination(BitcoinAddress, DestinationType.BitcoinAddress).Build();

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => _service.AddPaymentAsync(payment));
        Assert.Equal("No payment returned from server.", ex.Message);
        Assert.Equal(BitcoinAddress, payment.Destinations[0].Value);
    }

    [Fact]
    public async Task IsApiKeyValidAsync_ReturnsTrue_WhenClientReturnsTrue()
    {
        _clientMock
            .Setup(c => c.IsApiKeyValidAsync(It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Assert.True(await _service.IsApiKeyValidAsync());
    }

    [Fact]
    public async Task IsApiKeyValidAsync_ReturnsFalse_WhenClientReturnsFalse()
    {
        _clientMock
            .Setup(c => c.IsApiKeyValidAsync(It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Assert.False(await _service.IsApiKeyValidAsync());
    }

    [Fact]
    public async Task IsApiKeyValidAsync_ForwardsOptions_ToClient()
    {
        _clientMock
            .Setup(c => c.IsApiKeyValidAsync(_defaultOptions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.IsApiKeyValidAsync(options: _defaultOptions);

        _clientMock.Verify(c => c.IsApiKeyValidAsync(_defaultOptions, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsApiKeyValidAsync_ForwardsCancellationToken_ToClient()
    {
        using var cts = new CancellationTokenSource();
        _clientMock
            .Setup(c => c.IsApiKeyValidAsync(It.IsAny<BrantaApiOptions?>(), cts.Token))
            .ReturnsAsync(true);

        await _service.IsApiKeyValidAsync(cancellationToken: cts.Token);

        _clientMock.Verify(c => c.IsApiKeyValidAsync(It.IsAny<BrantaApiOptions?>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task AddPayment_WithMetadataAndBitcoinDestination_EncryptsMetadataWithDek()
    {
        const string dek = "generated-dek";
        const string encryptedMetadata = "encrypted-metadata";
        const string encryptedDek = "encrypted-dek-for-bitcoin";
        _secretGeneratorMock.SetupSequence(g => g.Generate()).Returns(dek).Returns(Secret);
        _aesEncryptionMock.Setup(e => e.Encrypt("{\"order\":\"1\"}", dek, false)).Returns(encryptedMetadata);
        _aesEncryptionMock.Setup(e => e.Encrypt(dek, Secret, false)).Returns(encryptedDek);

        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, DestinationType.BitcoinAddress)
            .AddMetadata("order", "1")
            .Build();
        var callerMetadata = payment.Metadata;
        await _service.AddPaymentAsync(payment);

        Assert.Equal(BitcoinAddress, payment.Destinations[0].Value);
        Assert.Equal(callerMetadata, payment.Metadata);
        Assert.Equal(encryptedMetadata, posted!.Metadata);
        Assert.Equal(encryptedDek, posted.Destinations[0].EncryptedDek);
    }

    [Fact]
    public async Task Verify_EncryptedBitcoinWithEncryptedDek_DecryptsMetadataViaDek()
    {
        const string encryptedDek = "encrypted-dek-blob";
        const string decryptedDek = "dek";
        const string encryptedMetadata = "encrypted-meta";
        const string decryptedMetadata = "{\"order\":\"1\"}";
        _aesEncryptionMock.Setup(e => e.Decrypt(encryptedDek, Secret)).Returns(decryptedDek);
        _aesEncryptionMock.Setup(e => e.Decrypt(encryptedMetadata, decryptedDek)).Returns(decryptedMetadata);

        var payment = new Payment
        {
            Metadata = encryptedMetadata,
            Destinations = [new Destination { Value = EncryptedBitcoinAddress, Type = DestinationType.BitcoinAddress, Id = "zk1", EncryptedDek = encryptedDek }]
        };
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await _service.VerifyAsync(BrantaBitcoinLookup());

        ExpectMatch(result);
        Assert.Equal(decryptedMetadata, result.Payments![0].Metadata);
        Assert.True(result.Payments[0].IsMetadataDecrypted);
    }

    [Fact]
    public async Task AddPayment_WithLeftoverDescription_EncryptsDescriptionInMetadata()
    {
        const string dek = "generated-dek";
        const string encryptedMetadata = "encrypted-metadata";
        const string encryptedDek = "encrypted-dek-for-bitcoin";
        _secretGeneratorMock.SetupSequence(g => g.Generate()).Returns(dek).Returns(Secret);
        _aesEncryptionMock.Setup(e => e.Encrypt("{\"description\":\"Invoice #1\"}", dek, false)).Returns(encryptedMetadata);
        _aesEncryptionMock.Setup(e => e.Encrypt(dek, Secret, false)).Returns(encryptedDek);

        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, DestinationType.BitcoinAddress)
            .Build();
        payment.Description = "Invoice #1";
        await _service.AddPaymentAsync(payment);

        Assert.Equal("Invoice #1", payment.Description);
        Assert.Equal(BitcoinAddress, payment.Destinations[0].Value);
        Assert.Null(posted!.Description);
        Assert.Equal(encryptedMetadata, posted.Metadata);
        Assert.Equal(encryptedDek, posted.Destinations[0].EncryptedDek);
    }

    [Fact]
    public async Task Verify_DecryptedMetadata_LiftsDescription()
    {
        const string encryptedDek = "encrypted-dek-blob";
        const string decryptedDek = "dek";
        const string encryptedMetadata = "encrypted-meta";
        const string decryptedMetadata = "{\"description\":\"Invoice #123\"}";
        _aesEncryptionMock.Setup(e => e.Decrypt(encryptedDek, Secret)).Returns(decryptedDek);
        _aesEncryptionMock.Setup(e => e.Decrypt(encryptedMetadata, decryptedDek)).Returns(decryptedMetadata);

        var payment = new Payment
        {
            Metadata = encryptedMetadata,
            Destinations = [new Destination { Value = EncryptedBitcoinAddress, Type = DestinationType.BitcoinAddress, Id = "zk1", EncryptedDek = encryptedDek }]
        };
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await _service.VerifyAsync(BrantaBitcoinLookup());

        ExpectMatch(result);
        Assert.Equal(decryptedMetadata, result.Payments![0].Metadata);
        Assert.Equal("Invoice #123", result.Payments[0].Description);
    }

    [Fact]
    public async Task AddPayment_Brand_CopyCarriesBrand_AndCallerUnchanged()
    {
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, DestinationType.BitcoinAddress)
            .SetBrand("Acme", "https://a/x.png", "https://a/y.png")
            .Build();
        var originalBrand = payment.Brand;

        await _service.AddPaymentAsync(payment);

        Assert.NotSame(payment, posted);
        Assert.NotSame(originalBrand, posted!.Brand);
        Assert.Equal("Acme", posted.Brand!.Name);
        Assert.Equal("https://a/x.png", posted.Brand.LogoUrl);
        Assert.Equal("https://a/y.png", posted.Brand.LogoLightUrl);
        Assert.Same(originalBrand, payment.Brand);
        Assert.Equal(BitcoinAddress, payment.Destinations[0].Value);
    }

#pragma warning disable CS0618 // Deprecated ChildPlatform under test.
    [Fact]
    public async Task AddPayment_OldStyleChildPlatform_CopyIsNormalizedToBrand_AndCallerUnchanged()
    {
        Payment? posted = null;
        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, BrantaApiOptions?, CancellationToken>((p, _, _) => posted = p)
            .ReturnsAsync(() => posted);

        var payment = new Payment
        {
            Destinations = [new Destination { Value = BitcoinAddress, Type = DestinationType.BitcoinAddress, Id = "zk1" }],
            ChildPlatform = new Platform { Name = "Acme" }
        };
        var originalChild = payment.ChildPlatform;

        await _service.AddPaymentAsync(payment);

        Assert.Equal("Acme", posted!.Brand!.Name);
        Assert.NotSame(originalChild, posted.Brand);
        Assert.Same(originalChild, payment.ChildPlatform);
        Assert.Same(originalChild, payment.Brand);
    }
#pragma warning restore CS0618
}
