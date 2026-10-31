using System.Net;
using System.Text;
using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.V2.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace Branta.Tests.V2.Services;

public class BrantaClientTest
{
    private const string SameOrigin = "http://localhost:3000";
    private const string OtherOrigin = "https://attacker.example";

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;

        public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;
        public FakeHttpMessageHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_exception != null) throw _exception;
            return Task.FromResult(_response!);
        }
    }

    private static BrantaClient CreateClient(HttpMessageHandler handler)
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));

        var options = Options.Create(new BrantaApiOptions
        {
            Environment = BrantaEnvironment.Localhost
        });

        return new BrantaClient(factoryMock.Object, options);
    }

    private static BrantaClient CreateClient(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };
        return CreateClient(new FakeHttpMessageHandler(response));
    }

    private const string Destinations = """[{"value":"test-destination"}]""";

    [Fact]
    public async Task GetPaymentsAsync_ChecksEveryPaymentsLogoUrl_NotJustTheFirst()
    {
        var json = $$"""
        [
            {"destinations": {{Destinations}} },
            {"destinations": {{Destinations}}, "platform_logo_url": "{{OtherOrigin}}/evil.png"}
        ]
        """;
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("platformLogoUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedPlatformLogoLightUrl_Throws()
    {
        var json = $$"""[{"destinations": {{Destinations}}, "platform_logo_light_url": "{{OtherOrigin}}/evil.png"}]""";
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("platformLogoLightUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedParentPlatformLogoUrl_Throws()
    {
        var json = $$"""[{"destinations": {{Destinations}}, "parent_platform": {"logo_url": "{{OtherOrigin}}/evil.png"} }]""";
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("parentPlatform.logoUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedParentPlatformLogoLightUrl_Throws()
    {
        var json = $$"""[{"destinations": {{Destinations}}, "parent_platform": {"logo_light_url": "{{OtherOrigin}}/evil.png"} }]""";
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("parentPlatform.logoLightUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedChildPlatformLogoUrl_Throws()
    {
        var json = $$"""[{"destinations": {{Destinations}}, "child_platform": {"logo_url": "{{OtherOrigin}}/evil.png"} }]""";
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("childPlatform.logoUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedChildPlatformLogoLightUrl_Throws()
    {
        var json = $$"""[{"destinations": {{Destinations}}, "child_platform": {"logo_light_url": "{{OtherOrigin}}/evil.png"} }]""";
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("childPlatform.logoLightUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_AllLogoFieldsSameOriginOrAbsent_DoesNotThrow()
    {
        var json = $$"""
        [
            {
                "destinations": {{Destinations}},
                "platform_logo_url": "{{SameOrigin}}/a.png",
                "platform_logo_light_url": "{{SameOrigin}}/b.png",
                "parent_platform": {"logo_url": "{{SameOrigin}}/c.png", "logo_light_url": "{{SameOrigin}}/d.png"},
                "child_platform": {"logo_url": "{{SameOrigin}}/e.png"}
            },
            { "destinations": {{Destinations}} }
        ]
        """;
        var client = CreateClient(json);

        var payments = await client.GetPaymentsAsync("value");

        Assert.Equal(2, payments.Count);
    }

    [Fact]
    public async Task GetPaymentsAsync_404_ReturnsEmptyList()
    {
        var client = CreateClient("not found", HttpStatusCode.NotFound);

        var payments = await client.GetPaymentsAsync("value");

        Assert.Empty(payments);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task GetPaymentsAsync_NonSuccess_ThrowsIncompleteError(HttpStatusCode status)
    {
        var client = CreateClient("error", status);

        var ex = await Assert.ThrowsAsync<IncompleteError>(() => client.GetPaymentsAsync("value"));
        Assert.Equal(status.ToString(), ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_NetworkFailure_ThrowsIncompleteError()
    {
        var client = CreateClient(new FakeHttpMessageHandler(new HttpRequestException("Failed to fetch")));

        var ex = await Assert.ThrowsAsync<IncompleteError>(() => client.GetPaymentsAsync("value"));
        Assert.Equal("Failed to fetch", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_Canceled_IsNotWrappedAsIncompleteError()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var client = CreateClient("[]");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetPaymentsAsync("value", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task PostPaymentAsync_401_ThrowsUnauthorized()
    {
        var client = CreateClient("nope", HttpStatusCode.Unauthorized);
        var payment = new Branta.V2.Models.Payment
        {
            Destinations = [new Branta.V2.Models.Destination { Value = "test-destination" }]
        };

        var options = new BrantaApiOptions { ApiKey = "key" };
        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.PostPaymentAsync(payment, options));
        Assert.Equal("Unauthorized", ex.Message);
    }

    [Fact]
    public async Task PostPaymentAsync_500_ThrowsInternalServerError()
    {
        var client = CreateClient("nope", HttpStatusCode.InternalServerError);
        var payment = new Branta.V2.Models.Payment
        {
            Destinations = [new Branta.V2.Models.Destination { Value = "test-destination" }]
        };

        var options = new BrantaApiOptions { ApiKey = "key" };
        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.PostPaymentAsync(payment, options));
        Assert.Equal("InternalServerError", ex.Message);
    }
}
