using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Classes;
using Moq;
using System.Net;

namespace Branta.Tests.V2.Classes.BrantaClientTests;

public class GetPaymentsAsyncTests : DataBrantaClientTests
{
    [Fact]
    public async Task ShouldReturnPayment()
    {
        SetupHttpClient(SingleBitcoinAddress);

        var result = await _brantaClient.GetPaymentsAsync(BITCOIN_ADDRESS);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task NonSuccessStatusCode_ReturnsEmptyList()
    {
        var httpClient = SetupHttpClient(HttpStatusCode.NotFound, "");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _brantaClient.GetPaymentsAsync(BITCOIN_ADDRESS);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPaymentsAsync_NullContent_ReturnsEmptyList()
    {
        var httpClient = SetupHttpClient(HttpStatusCode.OK, null);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _brantaClient.GetPaymentsAsync(BITCOIN_ADDRESS);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPaymentsAsync_CustomOptions_UsesProvidedOptions()
    {
        var customOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Production,
            Privacy = PrivacyMode.Loose
        };
        var httpClient = SetupHttpClient([]);

        await _brantaClient.GetPaymentsAsync(BITCOIN_ADDRESS, customOptions);

        Assert.Equal(new Uri(BrantaServerBaseUrl.Production.GetUrl()), httpClient.BaseAddress);
    }

    [Fact]
    public async Task GetPaymentsAsync_UrlEncodesAddress()
    {
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _brantaClient.GetPaymentsAsync("addr+with+plus");

        Assert.Single(capturedRequests);
        Assert.Equal("/v2/payments/addr%2Bwith%2Bplus", capturedRequests[0].RequestUri?.AbsolutePath + capturedRequests[0].RequestUri?.Query);
    }

    [Fact]
    public async Task GetPaymentsAsync_PlatformLogoUrlDomainMismatch_ThrowsException()
    {
        var payment = new PaymentBuilder()
            .AddDestination(BITCOIN_ADDRESS)
            .SetPlatformLogoUrl("https://evil.com/logo.png")
            .Build();

        SetupHttpClient(payment);

        var exception = await Assert.ThrowsAsync<BrantaPaymentException>(() => _brantaClient.GetPaymentsAsync("addr"));
        Assert.Contains("platformLogoUrl domain does not match", exception.Message);
    }
}
