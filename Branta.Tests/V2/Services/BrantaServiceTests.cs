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
    private readonly BrantaClientOptions _defaultOptions;
    private readonly BrantaService _service;
    private readonly BrantaService _strictService;

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

    private static Payment PlainBitcoinPayment => new PaymentBuilder()
        .AddDestination(BitcoinAddress, type: DestinationType.BitcoinAddress)
        .Build();

    private static Payment ZkBitcoinPayment => new PaymentBuilder()
        .AddDestination(EncryptedBitcoinAddress, type: DestinationType.BitcoinAddress)
        .SetZk()
        .Build();

    private static Payment ZkBolt11Payment => new PaymentBuilder()
        .AddDestination(EncryptedBolt11, type: DestinationType.Bolt11)
        .SetZk()
        .Build();

    private static Payment PlainBolt11Payment => new PaymentBuilder()
        .AddDestination(Bolt11Invoice, type: DestinationType.Bolt11)
        .Build();

    private static Payment PlainArkPayment => new PaymentBuilder()
        .AddDestination(ArkAddress, type: DestinationType.ArkAddress)
        .Build();

    private static Payment ZkArkPayment => new PaymentBuilder()
        .AddDestination(EncryptedArkAddress, type: DestinationType.ArkAddress)
        .SetZk()
        .Build();

    public BrantaServiceTests()
    {
        _clientMock = new Mock<IBrantaClient>();
        _aesEncryptionMock = new Mock<IAesEncryption>();
        _secretGeneratorMock = new Mock<ISecretGenerator>();
        _secretGeneratorMock.Setup(g => g.Generate()).Returns(Secret);
        _secretGeneratorMock.Setup(g => g.DeterministicNonce).Returns(false);
        _defaultOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            Privacy = PrivacyMode.Loose
        };
        _service = new BrantaService(_clientMock.Object, _aesEncryptionMock.Object, Options.Create(_defaultOptions), _secretGeneratorMock.Object);

        var strictOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            Privacy = PrivacyMode.Strict
        };
        _strictService = new BrantaService(_clientMock.Object, _aesEncryptionMock.Object, Options.Create(strictOptions), _secretGeneratorMock.Object);

        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBitcoinAddress, Secret)).Returns(BitcoinAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(Bolt11Invoice, Bolt11Hash, true)).Returns(EncryptedBolt11);
        _aesEncryptionMock.Setup(e => e.Decrypt(EncryptedBolt11, Bolt11Hash)).Returns(DecryptedBolt11);
        _aesEncryptionMock.Setup(e => e.Encrypt(BitcoinAddress, Secret, false)).Returns(EncryptedBitcoinAddress);
        _aesEncryptionMock.Setup(e => e.Encrypt(ArkAddress, ArkHash, true)).Returns(EncryptedArkAddress);
    }

    #region GetPaymentsByQrCodeAsync

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_ZkBitcoinUri_UsesZkParams()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ZkBitcoinPayment]);

        var qrText = $"bitcoin:{BitcoinAddress}?branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}";
        var result = await _service.GetPaymentsByQrCodeAsync(qrText);

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(BitcoinAddress, result[0].Destinations[0].Value);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_PlainBitcoinUri_UsesAddress()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(BitcoinAddress, null, default))
            .ReturnsAsync([PlainBitcoinPayment]);

        var result = await _service.GetPaymentsByQrCodeAsync($"bitcoin:{BitcoinAddress}");

        _clientMock.Verify(c => c.GetPaymentsAsync(BitcoinAddress, null, default), Times.Once);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_LightningBolt11Uri_UsesEncryptedInvoiceLookup()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBolt11Payment]);

        await _service.GetPaymentsByQrCodeAsync($"lightning:{Bolt11Invoice}");

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_CombinedZkQr_DecryptsBothAddressAndInvoice()
    {
        var payment = new PaymentBuilder()
            .AddDestination(EncryptedBitcoinAddress, type: DestinationType.BitcoinAddress)
            .SetZk()
            .AddDestination(EncryptedBolt11, type: DestinationType.Bolt11)
            .SetZk()
            .AddDestination(EncryptedArkAddress, type: DestinationType.ArkAddress)
            .SetZk()
            .Build();

        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var qrText = $"bitcoin:{BitcoinAddress}?branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}&lightning={Bolt11Invoice}&ark={ArkAddress}";
        var result = await _service.GetPaymentsByQrCodeAsync(qrText);

        var zkId = payment.Destinations[0].ZkId!;
        var bolt11ZkId = payment.Destinations[1].ZkId!;
        var arkZkId = payment.Destinations[2].ZkId!;
        Assert.Single(result);
        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}#k-{zkId}={Secret}&k-{bolt11ZkId}={Bolt11Hash}&k-{arkZkId}={ArkHash}", result[0].VerifyUrl);
        Assert.Equal(BitcoinAddress, result[0].Destinations[0].Value);
        Assert.Equal(DecryptedBolt11, result[0].Destinations[1].Value);
        _aesEncryptionMock.Verify(e => e.Decrypt(EncryptedBitcoinAddress, Secret), Times.Once);
        _aesEncryptionMock.Verify(e => e.Decrypt(EncryptedBolt11, Bolt11Hash), Times.Once);
    }

    #endregion

    #region GetPaymentsAsync

    [Fact]
    public async Task GetPaymentsAsync_ShouldReturnPayments_WhenClientSucceeds()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(BitcoinAddress, null, default))
            .ReturnsAsync([PlainBitcoinPayment]);

        var result = await _service.GetPaymentsAsync(BitcoinAddress);

        Assert.Single(result);
        Assert.Equal(BitcoinAddress, result[0].Destinations[0].Value);
    }

    [Fact]
    public async Task GetPaymentsAsync_ShouldReturnEmptyList_WhenClientReturnsEmpty()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetPaymentsAsync(BitcoinAddress);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPaymentsAsync_ShouldForwardOptions_ToClient()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(It.IsAny<string>(), _defaultOptions, It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBitcoinPayment]);

        await _service.GetPaymentsAsync(BitcoinAddress, options: _defaultOptions);

        _clientMock.Verify(c => c.GetPaymentsAsync(
            It.IsAny<string>(),
            _defaultOptions,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaymentsAsync_ShouldForwardCancellationToken_ToClient()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _clientMock
            .Setup(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaClientOptions?>(), token))
            .ReturnsAsync([]);

        await _service.GetPaymentsAsync(BitcoinAddress, ct: token);

        _clientMock.Verify(c => c.GetPaymentsAsync(
            It.IsAny<string>(),
            It.IsAny<BrantaClientOptions?>(),
            token), Times.Once);
    }

    [Fact]
    public async Task GetPaymentsAsync_ZkBitcoinAddress_DecryptsDestinationValue()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ZkBitcoinPayment]);

        var result = await _service.GetPaymentsAsync(EncryptedBitcoinAddress, destinationEncryptionKey: Secret);

        Assert.Single(result);
        Assert.Equal(BitcoinAddress, result[0].Destinations[0].Value);
        _aesEncryptionMock.Verify(e => e.Decrypt(EncryptedBitcoinAddress, Secret), Times.Once);
    }

    [Fact]
    public async Task GetPaymentsAsync_ZkBitcoinAddress_NoKey_ThrowsException()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ZkBitcoinPayment]);

        await Assert.ThrowsAsync<Exception>(() =>
            _service.GetPaymentsAsync(EncryptedBitcoinAddress, destinationEncryptionKey: null));

        _aesEncryptionMock.Verify(e => e.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsAsync_NonZkDestination_DoesNotDecrypt()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBitcoinPayment]);

        var result = await _service.GetPaymentsAsync(BitcoinAddress, destinationEncryptionKey: Secret);

        Assert.Equal(BitcoinAddress, result[0].Destinations[0].Value);
        _aesEncryptionMock.Verify(e => e.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsAsync_ZkBolt11_WithBolt11DestinationValue_DecryptsUsingHash()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ZkBolt11Payment]);

        var result = await _service.GetPaymentsAsync(Bolt11Invoice);

        Assert.Single(result);
        Assert.Equal(DecryptedBolt11, result[0].Destinations[0].Value);
        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        _aesEncryptionMock.Verify(e => e.Decrypt(EncryptedBolt11, Bolt11Hash), Times.Once);
    }

    [Fact]
    public async Task GetPaymentsAsync_ZkBolt11_WithNonBolt11DestinationValue_DoesNotDecrypt()
    {
        const string nonBolt11 = "not-a-bolt11-value";
        _clientMock
            .Setup(c => c.GetPaymentsAsync(nonBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ZkBolt11Payment]);

        var result = await _service.GetPaymentsAsync(nonBolt11);

        Assert.Equal(EncryptedBolt11, result[0].Destinations[0].Value);
        _clientMock.Verify(c => c.GetPaymentsAsync(nonBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        _aesEncryptionMock.Verify(e => e.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsAsync_NonZkBolt11_DoesNotDecrypt()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBolt11Payment]);

        var result = await _service.GetPaymentsAsync(Bolt11Invoice);

        Assert.Equal(Bolt11Invoice, result[0].Destinations[0].Value);
        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        _aesEncryptionMock.Verify(e => e.Decrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsAsync_PlainBitcoinAddress_SetsVerifyUrl()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(BitcoinAddress, null, default))
            .ReturnsAsync([PlainBitcoinPayment]);

        var result = await _service.GetPaymentsAsync(BitcoinAddress);

        Assert.Equal($"http://localhost:3000/v2/verify/{BitcoinAddress}", result[0].VerifyUrl);
    }

    [Fact]
    public async Task GetPaymentsAsync_PlainBolt11_SetsVerifyUrl()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _clientMock
            .Setup(c => c.GetPaymentsAsync(Bolt11Invoice, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBolt11Payment]);

        var result = await _service.GetPaymentsAsync(Bolt11Invoice);

        Assert.Equal($"http://localhost:3000/v2/verify/{Bolt11Invoice}", result[0].VerifyUrl);
    }

    [Fact]
    public async Task GetPaymentsAsync_ZkBitcoinAddress_SetsVerifyUrlWithKeyFragment()
    {
        var payment = new PaymentBuilder()
            .AddDestination(EncryptedBitcoinAddress, type: DestinationType.BitcoinAddress)
            .SetZk()
            .Build();
        var zkId = payment.Destinations[0].ZkId!;

        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await _service.GetPaymentsAsync(EncryptedBitcoinAddress, destinationEncryptionKey: Secret);

        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}#k-{zkId}={Secret}", result[0].VerifyUrl);
    }

    [Fact]
    public async Task GetPaymentsAsync_ZkBolt11_SetsVerifyUrlWithKeyFragment()
    {
        var payment = new PaymentBuilder()
            .AddDestination(EncryptedBolt11, type: DestinationType.Bolt11)
            .SetZk()
            .Build();
        var zkId = payment.Destinations[0].ZkId!;

        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await _service.GetPaymentsAsync(Bolt11Invoice);

        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBolt11}#k-{zkId}={Bolt11Hash}", result[0].VerifyUrl);
    }

    [Fact]
    public async Task GetPaymentsAsync_ZkBitcoinAndBolt11_SetsVerifyUrlWithBothKeyFragments()
    {
        var payment = new PaymentBuilder()
            .AddDestination(EncryptedBitcoinAddress, type: DestinationType.BitcoinAddress)
            .SetZk()
            .AddDestination(EncryptedBolt11, type: DestinationType.Bolt11)
            .SetZk()
            .Build();

        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await _service.GetPaymentsAsync(Bolt11Invoice, destinationEncryptionKey: Secret);

        var zkIdBitcoin = payment.Destinations[0].ZkId!;
        var zkIdBolt11 = payment.Destinations[1].ZkId!;
        Assert.Equal(
            $"http://localhost:3000/v2/verify/{EncryptedBolt11}#k-{zkIdBitcoin}={Secret}&k-{zkIdBolt11}={Bolt11Hash}",
            result[0].VerifyUrl);

        result = await _service.GetPaymentsAsync(EncryptedBitcoinAddress, destinationEncryptionKey: Secret);

        zkIdBitcoin = payment.Destinations[0].ZkId!;
        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}#k-{zkIdBitcoin}={Secret}", result[0].VerifyUrl);
    }

    #endregion

    #region AddPaymentAsync

    [Fact]
    public async Task AddPaymentAsync_PlainDestination_DoesNotEncrypt()
    {
        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, type: DestinationType.BitcoinAddress)
            .Build();

        _clientMock
            .Setup(c => c.PostPaymentAsync(payment, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlainBitcoinPayment);

        await _service.AddPaymentAsync(payment);

        _aesEncryptionMock.Verify(e => e.Encrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task AddPaymentAsync_ZkBitcoinAddress_EncryptsWithSecret()
    {
        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, type: DestinationType.BitcoinAddress)
            .SetZk()
            .Build();
        var zkId = payment.Destinations[0].ZkId!;

        var responsePayment = new PaymentBuilder()
            .AddDestination(EncryptedBitcoinAddress, type: DestinationType.BitcoinAddress)
            .Build();
        responsePayment.Destinations[0].IsZk = true;
        responsePayment.Destinations[0].ZkId = zkId;

        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responsePayment);

        var (result, secret) = await _service.AddPaymentAsync(payment);

        _aesEncryptionMock.Verify(e => e.Encrypt(BitcoinAddress, Secret, false), Times.Once);
        Assert.Equal(Secret, secret);
        Assert.Equal(EncryptedBitcoinAddress, payment.Destinations[0].Value);
    }

    [Fact]
    public async Task AddPaymentAsync_ZkBolt11_EncryptsWithHash()
    {
        var payment = new PaymentBuilder()
            .AddDestination(Bolt11Invoice, type: DestinationType.Bolt11)
            .SetZk()
            .Build();
        var zkId = payment.Destinations[0].ZkId!;

        var responsePayment = new PaymentBuilder()
            .AddDestination(EncryptedBolt11, type: DestinationType.Bolt11)
            .Build();
        responsePayment.Destinations[0].IsZk = true;
        responsePayment.Destinations[0].ZkId = zkId;

        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responsePayment);

        await _service.AddPaymentAsync(payment);

        _aesEncryptionMock.Verify(e => e.Encrypt(Bolt11Invoice, Bolt11Hash, true), Times.Once);
        Assert.Equal(EncryptedBolt11, payment.Destinations[0].Value);
    }

    [Fact]
    public async Task AddPaymentAsync_ZkArkAddress_EncryptsWithHash()
    {
        var payment = new PaymentBuilder()
            .AddDestination(ArkAddress, type: DestinationType.ArkAddress)
            .SetZk()
            .Build();
        var zkId = payment.Destinations[0].ZkId!;

        var responsePayment = new PaymentBuilder()
            .AddDestination(EncryptedArkAddress, type: DestinationType.ArkAddress)
            .Build();
        responsePayment.Destinations[0].IsZk = true;
        responsePayment.Destinations[0].ZkId = zkId;

        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responsePayment);

        await _service.AddPaymentAsync(payment);

        _aesEncryptionMock.Verify(e => e.Encrypt(ArkAddress, ArkHash, true), Times.Once);
        Assert.Equal(EncryptedArkAddress, payment.Destinations[0].Value);
    }

    [Fact]
    public async Task AddPaymentAsync_ZkBitcoinAddress_SetsVerifyUrlWithKeyFragment()
    {
        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, type: DestinationType.BitcoinAddress)
            .SetZk()
            .Build();
        var zkId = payment.Destinations[0].ZkId!;

        var responsePayment = new PaymentBuilder()
            .AddDestination(EncryptedBitcoinAddress, type: DestinationType.BitcoinAddress)
            .Build();
        responsePayment.Destinations[0].IsZk = true;
        responsePayment.Destinations[0].ZkId = zkId;

        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responsePayment);

        var (result, _) = await _service.AddPaymentAsync(payment);

        Assert.Equal($"http://localhost:3000/v2/verify/{EncryptedBitcoinAddress}#k-{zkId}={Secret}", result.VerifyUrl);
    }

    [Fact]
    public async Task AddPaymentAsync_ReturnsGeneratedSecret()
    {
        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, type: DestinationType.BitcoinAddress)
            .SetZk()
            .Build();

        var responsePayment = new PaymentBuilder()
            .AddDestination(EncryptedBitcoinAddress, type: DestinationType.BitcoinAddress)
            .Build();
        responsePayment.Destinations[0].IsZk = true;
        responsePayment.Destinations[0].ZkId = payment.Destinations[0].ZkId!;

        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responsePayment);

        var (_, secret) = await _service.AddPaymentAsync(payment);

        Assert.Equal(Secret, secret);
    }

    [Fact]
    public async Task AddPaymentAsync_UnsupportedZkType_Throws()
    {
        var payment = new PaymentBuilder()
            .AddDestination("0xdeadbeef", type: DestinationType.TetherAddress)
            .SetZk()
            .Build();

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _service.AddPaymentAsync(payment));
        _clientMock.Verify(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region IsApiKeyValidAsync

    [Fact]
    public async Task IsApiKeyValidAsync_ReturnsTrue_WhenClientReturnsTrue()
    {
        _clientMock
            .Setup(c => c.IsApiKeyValidAsync(It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.IsApiKeyValidAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsApiKeyValidAsync_ReturnsFalse_WhenClientReturnsFalse()
    {
        _clientMock
            .Setup(c => c.IsApiKeyValidAsync(It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.IsApiKeyValidAsync();

        Assert.False(result);
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
        var token = cts.Token;

        _clientMock
            .Setup(c => c.IsApiKeyValidAsync(It.IsAny<BrantaClientOptions?>(), token))
            .ReturnsAsync(true);

        await _service.IsApiKeyValidAsync(ct: token);

        _clientMock.Verify(c => c.IsApiKeyValidAsync(It.IsAny<BrantaClientOptions?>(), token), Times.Once);
    }

    #endregion

    #region StrictMode

    [Fact]
    public async Task GetPaymentsAsync_StrictMode_BitcoinAddress_Throws()
    {
        await Assert.ThrowsAsync<BrantaPaymentException>(() =>
            _strictService.GetPaymentsAsync(BitcoinAddress));

        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsAsync_StrictMode_Bolt11Invoice_DoesNotThrow_UsesEncryptedLookup()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ZkBolt11Payment]);

        await _strictService.GetPaymentsAsync(Bolt11Invoice);

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaymentsAsync_StrictMode_ArkAddress_DoesNotThrow_UsesEncryptedLookup()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedArkAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ZkArkPayment]);

        await _strictService.GetPaymentsAsync(ArkAddress);

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedArkAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaymentsAsync_StrictMode_Bolt11_NoFallbackToPlainText()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _clientMock
            .Setup(c => c.GetPaymentsAsync(Bolt11Invoice, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBolt11Payment]);

        var result = await _strictService.GetPaymentsAsync(Bolt11Invoice);

        Assert.Empty(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.GetPaymentsAsync(Bolt11Invoice, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_StrictMode_PlainBitcoinUri_ReturnsEmptyList()
    {
        var result = await _strictService.GetPaymentsByQrCodeAsync($"bitcoin:{BitcoinAddress}");

        Assert.Empty(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(It.IsAny<string>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_StrictMode_ZkBitcoinUri_Succeeds()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ZkBitcoinPayment]);

        var qrText = $"bitcoin:{BitcoinAddress}?branta_id={EncryptedBitcoinAddress}&branta_secret={Secret}";
        var result = await _strictService.GetPaymentsByQrCodeAsync(qrText);

        Assert.Single(result);
        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBitcoinAddress, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_StrictMode_LightningBolt11Uri_Succeeds()
    {
        _clientMock
            .Setup(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlainBolt11Payment]);

        await _strictService.GetPaymentsByQrCodeAsync($"lightning:{Bolt11Invoice}");

        _clientMock.Verify(c => c.GetPaymentsAsync(EncryptedBolt11, It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddPaymentAsync_StrictMode_PlainDestination_Throws()
    {
        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, type: DestinationType.BitcoinAddress)
            .Build();

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _strictService.AddPaymentAsync(payment));

        _clientMock.Verify(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPaymentAsync_StrictMode_AllZkDestinations_Succeeds()
    {
        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, type: DestinationType.BitcoinAddress)
            .SetZk()
            .Build();
        var zkId = payment.Destinations[0].ZkId!;

        var responsePayment = new PaymentBuilder()
            .AddDestination(EncryptedBitcoinAddress, type: DestinationType.BitcoinAddress)
            .Build();
        responsePayment.Destinations[0].IsZk = true;
        responsePayment.Destinations[0].ZkId = zkId;

        _clientMock
            .Setup(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responsePayment);

        await _strictService.AddPaymentAsync(payment);

        _clientMock.Verify(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddPaymentAsync_StrictMode_MixedDestinations_Throws()
    {
        var payment = new PaymentBuilder()
            .AddDestination(BitcoinAddress, type: DestinationType.BitcoinAddress)
            .SetZk()
            .AddDestination(Bolt11Invoice, type: DestinationType.Bolt11)
            .Build();

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _strictService.AddPaymentAsync(payment));

        _clientMock.Verify(c => c.PostPaymentAsync(It.IsAny<Payment>(), It.IsAny<BrantaClientOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
