using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Classes;
using Branta.V2.Interfaces;
using Branta.V2.Models;
using Branta.V2.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace Branta.Tests.V2.Services;

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
    private const string DecryptedBolt11 = "lnbc100n1pdecrypted";
    private static readonly string Bolt11Hash = Bolt11Invoice.ToNormalizedHash();

    public const string ArkAddress = "ark100testaddress";
    public const string EncryptedArkAddress = "encrypted-ark-address";
    private static readonly string ArkHash = ArkAddress.ToNormalizedHash();

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
        _aesEncryptionMock.Setup(e => e.Encrypt(Bolt11Invoice, Bolt11Hash, true)).Returns(EncryptedBolt11);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBolt11, Bolt11Hash)).Returns(DecryptedBolt11);
        _aesEncryptionMock.Setup(e => e.Encrypt(BitcoinAddress, Secret, false)).Returns(EncryptedBitcoinAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(ArkAddress, ArkHash, true)).Returns(EncryptedArkAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(Bolt11Invoice.ToLowerInvariant(), Bolt11Hash, true)).Returns(EncryptedBolt11);
        _aesEncryptionMock.Setup(e => e.Encrypt(ArkAddress.ToLowerInvariant(), ArkHash, true)).Returns(EncryptedArkAddress);
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
        Assert.Equal(DecryptedBolt11, result.Payments![0].Destinations[0].Value);
    }

    [Fact]
    public async Task Verify_UppercaseLightningBolt11Uri_UsesEncryptedInvoiceLookup()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([EncryptedBolt11Payment()]);

        await _service.VerifyAsync($"lightning:{Bolt11Invoice.ToUpperInvariant()}");

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaApiOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.Equal(DecryptedBolt11, result.Payments[0].Destinations[1].Value);
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

        ExpectMatch(await _service.VerifyAsync(BrantaBitcoinLookup()));
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

        var qr = $"bitcoin:{BitcoinAddress}?lightning={Bolt11Invoice}&branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}";
        ExpectUnknown(await _service.VerifyAsync(qr));
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
        Assert.Equal(DecryptedBolt11, result.Payments![0].Destinations[0].Value);
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

        Assert.Equal(DestinationType.BitcoinAddress, posted!.Destinations[0].Type);
        Assert.Equal(EncryptedBitcoinAddress, posted.Destinations[0].Value);
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
        await _service.AddPaymentAsync(payment);

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
}
