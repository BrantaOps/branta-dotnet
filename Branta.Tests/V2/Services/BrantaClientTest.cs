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
    // Matches BrantaServerBaseUrl.Localhost's [Url] attribute.
    private const string SameOrigin = "http://localhost:3000";
    private const string OtherOrigin = "https://attacker.example";

    private class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private static BrantaClient CreateClient(string responseJson)
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));

        var options = Options.Create(new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            Privacy = PrivacyMode.Loose
        });

        return new BrantaClient(factoryMock.Object, options);
    }

    private const string Destinations = """[{"value":"test-destination"}]""";

    [Fact]
    public async Task GetPaymentsAsync_ChecksEveryPaymentsLogoUrl_NotJustTheFirst()
    {
        var json = $$"""
        [
            {"destinations": {{Destinations}} },
            {"destinations": {{Destinations}}, "platform_logo_url": "{{OtherOrigin}}/logo.png" }
        ]
        """;
        var client = CreateClient(json);

        await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedPlatformLogoLightUrl_Throws()
    {
        var json = $$"""
        [ {"destinations": {{Destinations}}, "platform_logo_light_url": "{{OtherOrigin}}/logo-light.png" } ]
        """;
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("platformLogoLightUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedParentPlatformLogoUrl_Throws()
    {
        var json = $$"""
        [ {"destinations": {{Destinations}}, "parent_platform": {"logo_url": "{{OtherOrigin}}/logo.png"} } ]
        """;
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("parentPlatform.logoUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedParentPlatformLogoLightUrl_Throws()
    {
        var json = $$"""
        [ {"destinations": {{Destinations}}, "parent_platform": {"logo_light_url": "{{OtherOrigin}}/logo-light.png"} } ]
        """;
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("parentPlatform.logoLightUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedChildPlatformLogoUrl_Throws()
    {
        var json = $$"""
        [ {"destinations": {{Destinations}}, "child_platform": {"logo_url": "{{OtherOrigin}}/logo.png"} } ]
        """;
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("childPlatform.logoUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedChildPlatformLogoLightUrl_Throws()
    {
        var json = $$"""
        [ {"destinations": {{Destinations}}, "child_platform": {"logo_light_url": "{{OtherOrigin}}/logo-light.png"} } ]
        """;
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
}
