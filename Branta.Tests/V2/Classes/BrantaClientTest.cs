using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Classes;
using Branta.V2.Models;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Branta.Tests.V2.Classes;

public class BrantaClientTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IOptions<BrantaClientOptions>> _optionsMock;
    private readonly BrantaClientOptions _defaultOptions;
    private readonly BrantaClient _sut;

    private readonly List<Payment> _testPayments = new List<Payment>
        {
            new() {
                Destinations =
                [
                    new Destination()
                    {
                        Value = "123",
                        IsZk = false
                    }
                ]
            },
            new() {
                Destinations =
                [
                    new Destination()
                    {
                        Value = "456",
                        IsZk = false
                    }
                ]
            },
        };

    public BrantaClientTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _optionsMock = new Mock<IOptions<BrantaClientOptions>>();
        _defaultOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key"
        };
        _optionsMock.Setup(x => x.Value).Returns(_defaultOptions);
        _sut = new BrantaClient(_httpClientFactoryMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task GetPaymentAsync_ShouldReturnPayments()
    {
        var address = "test-address";
        var jsonResponse = JsonSerializer.Serialize(_testPayments);
        var httpClient = SetupHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.GetPaymentsAsync(address);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("123", result.First().Destinations.First().Value);
        Assert.Equal("456", result.Last().Destinations.First().Value);
    }

    [Fact]
    public async Task GetPaymentsAsync_NonSuccessStatusCode_ReturnsEmptyList()
    {
        var address = "test-address";
        var httpClient = SetupHttpClient(HttpStatusCode.NotFound, "");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.GetPaymentsAsync(address);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPaymentsAsync_NullContent_ReturnsEmptyList()
    {
        var address = "test-address";
        var httpClient = SetupHttpClient(HttpStatusCode.OK, null);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.GetPaymentsAsync(address);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPaymentsAsync_CustomOptions_UsesProvidedOptions()
    {
        var address = "test-address";
        var customOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Production
        };
        var httpClient = SetupHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.GetPaymentsAsync(address, customOptions);

        Assert.Equal(new Uri(BrantaServerBaseUrl.Production.GetUrl()), httpClient.BaseAddress);
    }

    [Fact]
    public async Task GetZKPaymentAsync_WithZkDestinations_DecryptsValues()
    {
        var encryptedValue = "pQerSFV+fievHP+guYoGJjx1CzFFrYWHAgWrLhn5473Z19M6+WMScLd1hsk808AEF/x+GpZKmNacFBf5BbQ=";
        var payments = new List<Payment>
        {
            new() {
                Destinations =
                [
                    new Destination { IsZk = true, Value = encryptedValue },
                    new Destination { IsZk = false, Value = "plain-value" }
                ]
            }
        };
        var jsonResponse = JsonSerializer.Serialize(payments);
        var httpClient = SetupHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);


        var result = await _sut.GetZKPaymentAsync(encryptedValue, "1234");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", result.First().Destinations.First().Value);
    }

    [Fact]
    public async Task GetZKPaymentAsync_NoZkDestinations_ReturnsUnmodified()
    {
        var payments = new List<Payment>
        {
            new() {
                Destinations =
                [
                    new Destination { IsZk = false, Value = "plain-value" }
                ]
            }
        };
        var jsonResponse = JsonSerializer.Serialize(payments);
        var httpClient = SetupHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.GetZKPaymentAsync("plain-value", "test-secret");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("plain-value", result[0].Destinations[0].Value);
    }

    [Fact]
    public async Task AddPaymentAsync_NoApiKey_ThrowsException()
    {
        var payment = _testPayments.First();
        var optionsWithoutApiKey = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Production,
            DefaultApiKey = null
        };
        _optionsMock.Setup(x => x.Value).Returns(optionsWithoutApiKey);
        var sut = new BrantaClient(_httpClientFactoryMock.Object, _optionsMock.Object);
        var httpClient = new HttpClient();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<BrantaPaymentException>(() => sut.AddPaymentAsync(payment));
        Assert.Equal("Unauthorized", exception.Message);
    }

    [Fact]
    public async Task AddPaymentAsync_UsesCustomApiKey()
    {
        var payment = _testPayments.First();
        var customOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Production,
            DefaultApiKey = "custom-api-key"
        };
        var jsonResponse = JsonSerializer.Serialize(_testPayments.First());
        var httpClient = SetupHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.AddPaymentAsync(payment, customOptions);

        Assert.Equal("Bearer", httpClient.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal("custom-api-key", httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }

    private HttpClient SetupHttpClient(HttpStatusCode statusCode, string? content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = content != null ? new StringContent(content, Encoding.UTF8, "application/json") : null
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(handlerMock.Object);
    }
}
