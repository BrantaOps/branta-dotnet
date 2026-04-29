using Branta.Classes;
using Branta.Enums;
using Branta.V2.Classes;
using Branta.V2.Models;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Branta.Tests.V2.Classes.BrantaClientTests;

public class BaseBrantaClientTests
{
    protected readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    protected readonly Mock<IOptions<BrantaClientOptions>> _optionsMock;
    protected readonly Mock<ISecretGenerator> _secretGeneratorMock;
    protected readonly BrantaClientOptions _defaultOptions;
    protected readonly BrantaClient _brantaClient;

    protected static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public BaseBrantaClientTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _optionsMock = new Mock<IOptions<BrantaClientOptions>>();
        _secretGeneratorMock = new Mock<ISecretGenerator>();
        _defaultOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            Privacy = PrivacyMode.Loose
        };
        _optionsMock.Setup(x => x.Value).Returns(_defaultOptions);
        _brantaClient = new BrantaClient(_httpClientFactoryMock.Object, _optionsMock.Object, _secretGeneratorMock.Object);
    }

    protected HttpClient SetupHttpClient(Payment payment)
    {
        return SetupHttpClient([payment]);
    }

    protected HttpClient SetupHttpClientForSinglePayment(Payment payment)
    {
        var httpClient = SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(payment, SnakeCaseOptions));
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return httpClient;
    }

    protected (HttpClient, List<HttpRequestMessage>) SetupCapturingHttpClientForSinglePayment(Payment payment)
    {
        var (httpClient, captured) = SetupCapturingHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(payment, SnakeCaseOptions));
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return (httpClient, captured);
    }

    protected HttpClient SetupHttpClient(List<Payment> payments)
    {
        var httpClient = SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(payments, SnakeCaseOptions));
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return httpClient;
    }

    protected static HttpClient SetupHttpClient(HttpStatusCode statusCode, string? content)
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

    protected static (HttpClient, List<HttpRequestMessage>) SetupCapturingHttpClient(HttpStatusCode statusCode, string? content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var capturedRequests = new List<HttpRequestMessage>();
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
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
            .ReturnsAsync(response);

        return (new HttpClient(handlerMock.Object), capturedRequests);
    }
}
