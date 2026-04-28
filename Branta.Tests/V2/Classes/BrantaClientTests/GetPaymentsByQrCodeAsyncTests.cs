using Branta.Classes;
using Branta.Enums;
using Moq;
using System.Net;

namespace Branta.Tests.V2.Classes.BrantaClientTests;

public class GetPaymentsByQrCodeAsyncTests : DataBrantaClientTests
{
    [Fact]
    public async Task PlainBitcoinAddress_ReturnsPayment()
    {
        SetupHttpClient(SingleBitcoinAddress);

        var result = await _brantaClient.GetPaymentsByQrCodeAsync(BITCOIN_ADDRESS);

        Assert.Single(result);
        Assert.Equal(BITCOIN_ADDRESS, result.First().GetDefaultValue());
    }

    [Fact]
    public async Task BitcoinUri_ReturnsPayment()
    {
        SetupHttpClient(SingleBitcoinAddress);

        var result = await _brantaClient.GetPaymentsByQrCodeAsync($"bitcoin:{BITCOIN_ADDRESS}");

        Assert.Single(result);
        Assert.Equal(BITCOIN_ADDRESS, result.First().GetDefaultValue());
    }

    [Fact]
    public async Task LightningUri_ReturnsPayment()
    {
        SetupHttpClient(SingleBolt11Invoice);

        var result = await _brantaClient.GetPaymentsByQrCodeAsync($"lightning:{BOLT_11}");

        Assert.Single(result);
        Assert.Equal(BOLT_11, result.First().GetDefaultValue());
    }

    [Fact]
    public async Task ZkBitcoinUri_DecryptsPayment()
    {
        SetupHttpClient(EncryptedBitcoinAddress);
        var qrText = $"bitcoin:?branta_id={Uri.EscapeDataString(ENCRYPTED_BITCOIN_ADDRESS)}&branta_secret={ENCRYPTED_BITCOIN_SECRET}";

        var result = await _brantaClient.GetPaymentsByQrCodeAsync(qrText);

        Assert.Single(result);
        Assert.Equal(BITCOIN_ADDRESS, result.First().GetDefaultValue());
    }

    [Fact]
    public async Task NonSuccessStatusCode_ReturnsEmptyList()
    {
        var httpClient = SetupHttpClient(HttpStatusCode.NotFound, "");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _brantaClient.GetPaymentsByQrCodeAsync(BITCOIN_ADDRESS);

        Assert.Empty(result);
    }

    [Fact]
    public async Task StrictPrivacy_PlainAddress_ReturnsEmptyList()
    {
        var strictOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            Privacy = PrivacyMode.Strict
        };

        var result = await _brantaClient.GetPaymentsByQrCodeAsync(BITCOIN_ADDRESS, strictOptions);

        Assert.Empty(result);
    }

    [Fact]
    public async Task StrictPrivacy_Bolt11_ZK_DoesNotExist_ReturnsEmptyList()
    {
        var strictOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            Privacy = PrivacyMode.Strict
        };

        SetupHttpClient([]);
        var qrText = $"lightning:{BOLT_11}";

        var result = await _brantaClient.GetPaymentsByQrCodeAsync(BITCOIN_ADDRESS, strictOptions);

        Assert.Empty(result);
    }


    [Fact]
    public async Task StrictPrivacy_Bolt11_ZK_DoesExist_ReturnsPayment()
    {
        var strictOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            Privacy = PrivacyMode.Strict
        };

        SetupHttpClient(EncryptedBolt11Invoice);
        var qrText = $"lightning:{BOLT_11}";

        var result = await _brantaClient.GetPaymentsByQrCodeAsync(qrText, strictOptions);

        Assert.Equal(BOLT_11, result.First().GetDefaultValue());
    }
}
