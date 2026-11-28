using System.Net;
using System.Text;
using Branta;
using Microsoft.Extensions.Options;
using Moq;

namespace Branta.Tests.Services;

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

    private class CapturingHandler(HttpStatusCode status = HttpStatusCode.OK, string body = "[]") : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    private static string ProjectVersion()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Branta", "Branta.csproj")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        var csproj = File.ReadAllText(Path.Combine(dir!.FullName, "Branta", "Branta.csproj"));
        var match = System.Text.RegularExpressions.Regex.Match(csproj, @"<AssemblyVersion>(\d+\.\d+\.\d+)</AssemblyVersion>");
        Assert.True(match.Success);
        return match.Groups[1].Value;
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
    public async Task GetPaymentsAsync_MismatchedBrandLogoUrl_Throws()
    {
        var json = $$"""[{"destinations": {{Destinations}}, "child_platform": {"logo_url": "{{OtherOrigin}}/evil.png"} }]""";
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("brand.logoUrl", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_MismatchedBrandLogoLightUrl_Throws()
    {
        var json = $$"""[{"destinations": {{Destinations}}, "child_platform": {"logo_light_url": "{{OtherOrigin}}/evil.png"} }]""";
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("brand.logoLightUrl", ex.Message);
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
    public async Task GetPaymentsAsync_UnknownDestinationType_DoesNotThrow()
    {
        var json = """[{"destinations":[{"value":"addr","type":"not_a_destination_type","zk":true}]}]""";
        var client = CreateClient(json);

        var payments = await client.GetPaymentsAsync("value");

        Assert.Single(payments);
        Assert.Null(payments[0].Destinations[0].Type);
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
    public async Task GetPaymentsAsync_MalformedJson_ThrowsIncompleteError()
    {
        var client = CreateClient("{");

        var ex = await Assert.ThrowsAsync<IncompleteError>(() => client.GetPaymentsAsync("value"));
        Assert.Equal("MalformedResponse", ex.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_NonArrayBody_ReturnsEmptyList()
    {
        var client = CreateClient("""{"not":"an-array"}""");

        var payments = await client.GetPaymentsAsync("value");

        Assert.Empty(payments);
    }

    [Fact]
    public async Task GetPaymentsAsync_BadRowShape_ThrowsIncompleteError()
    {
        var client = CreateClient("[1]");

        var ex = await Assert.ThrowsAsync<IncompleteError>(() => client.GetPaymentsAsync("value"));
        Assert.Equal("MalformedResponse", ex.Message);
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("""[{"destinations":null}]""")]
    [InlineData("""[{"destinations":[null]}]""")]
    [InlineData("""[{"destinations":[{"value":"x"},null]}]""")]
    [InlineData("""[{"destinations":[{"value":"x"}]}, null]""")]
    public async Task GetPaymentsAsync_NullRowOrDestination_ThrowsIncompleteError(string json)
    {
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<IncompleteError>(() => client.GetPaymentsAsync("value"));
        Assert.Equal("MalformedResponse", ex.Message);
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("""[{"destinations":null}]""")]
    [InlineData("""[{"destinations":[null]}]""")]
    public async Task VerifyAsync_NullRowOrDestination_IsIncompleteWithVerifyUrl(string json)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new FakeHttpMessageHandler(response)));
        var service = new BrantaService(
            new BrantaApiOptions { Environment = BrantaEnvironment.Localhost },
            factoryMock.Object);

        var result = await service.VerifyAsync(
            "bitcoin:bc1qgw3dzmhnyvcswc9r0v0z0ajtp8ulm4nuyeahwr?branta_id=abc&branta_secret=secret");

        Assert.Equal(VerifyStatus.Incomplete, result.Status);
        Assert.StartsWith($"{SameOrigin}/v2/verify/abc", result.VerifyUrl);
    }

    [Fact]
    public async Task GetPaymentsAsync_Timeout_ThrowsIncompleteError()
    {
        var client = CreateClient(new FakeHttpMessageHandler(new TaskCanceledException("timed out")));

        var ex = await Assert.ThrowsAsync<IncompleteError>(() => client.GetPaymentsAsync("value"));
        Assert.Equal("timed out", ex.Message);
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
        var payment = new Payment
        {
            Destinations = [new Destination { Value = "test-destination" }]
        };

        var options = new BrantaApiOptions { ApiKey = "key" };
        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.PostPaymentAsync(payment, options));
        Assert.Equal("Unauthorized", ex.Message);
    }

    [Fact]
    public async Task PostPaymentAsync_500_ThrowsInternalServerError()
    {
        var client = CreateClient("nope", HttpStatusCode.InternalServerError);
        var payment = new Payment
        {
            Destinations = [new Destination { Value = "test-destination" }]
        };

        var options = new BrantaApiOptions { ApiKey = "key" };
        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.PostPaymentAsync(payment, options));
        Assert.Equal("InternalServerError", ex.Message);
    }

    [Fact]
    public async Task PostPaymentAsync_SendsSdkHeader_MatchingProjectVersion()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, "");
        var client = CreateClient(handler);
        var payment = new Payment
        {
            Destinations = [new Destination { Value = "test-destination" }]
        };

        await client.PostPaymentAsync(payment, new BrantaApiOptions { ApiKey = "key", HmacSecret = "secret" });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        var value = Assert.Single(request.Headers.GetValues("X-Branta-SDK"));
        Assert.Equal($"dotnet/{ProjectVersion()}", value);
    }

    private static async Task<string> PostBodyAsync(Payment payment)
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, "");
        var client = CreateClient(handler);

        await client.PostPaymentAsync(payment, new BrantaApiOptions { ApiKey = "key" });

        return Assert.Single(handler.Bodies);
    }

    private static Payment NewPayment() => new()
    {
        Destinations = [new Destination { Value = "test-destination" }]
    };

    private const string AcmeBrandJson = """{"name":"Acme","logo_url":"https://a/x.png","logo_light_url":"https://a/y.png"}""";

    [Fact]
    public async Task PostPaymentAsync_SetBrand_SendsBrandOnly()
    {
        var payment = new PaymentBuilder()
            .AddDestination("test-destination")
            .SetBrand("Acme", "https://a/x.png", "https://a/y.png")
            .Build();

        var body = await PostBodyAsync(payment);

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal(AcmeBrandJson, doc.RootElement.GetProperty("brand").GetRawText());
        Assert.False(doc.RootElement.TryGetProperty("child_platform", out _));
    }

#pragma warning disable CS0618 // Deprecated child platform aliases under test.
    [Fact]
    public async Task PostPaymentAsync_SetChildPlatform_SendsSameBodyAsSetBrand()
    {
        var viaBrand = new PaymentBuilder().AddDestination("d").SetBrand("Acme", "https://a/x.png", "https://a/y.png").Build();
        var viaAlias = new PaymentBuilder().AddDestination("d").SetChildPlatform("Acme", "https://a/x.png", "https://a/y.png").Build();
        viaBrand.Destinations[0].Id = viaAlias.Destinations[0].Id = "id";

        var brandBody = await PostBodyAsync(viaBrand);
        var aliasBody = await PostBodyAsync(viaAlias);

        Assert.Equal(brandBody, aliasBody);
        Assert.DoesNotContain("child_platform", aliasBody);
    }

    [Fact]
    public async Task PostPaymentAsync_OnlyDeprecatedChildPlatformProperty_SerializesAsBrand()
    {
        var payment = NewPayment();
        payment.ChildPlatform = new Platform { Name = "Acme", LogoUrl = "https://a/x.png", LogoLightUrl = "https://a/y.png" };

        var body = await PostBodyAsync(payment);

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal(AcmeBrandJson, doc.RootElement.GetProperty("brand").GetRawText());
        Assert.DoesNotContain("child_platform", body);
    }

    [Fact]
    public async Task PostPaymentAsync_BothBrandAndChildPlatform_BrandWins()
    {
        var payment = NewPayment();
        payment.ChildPlatform = new Platform { Name = "Old" };
        payment.Brand = new Platform { Name = "New" };

        var body = await PostBodyAsync(payment);

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("""{"name":"New"}""", doc.RootElement.GetProperty("brand").GetRawText());
        Assert.DoesNotContain("child_platform", body);
        Assert.DoesNotContain("Old", body);
    }

    [Fact]
    public async Task PostPaymentAsync_NoBrand_OmitsBothKeys()
    {
        var body = await PostBodyAsync(NewPayment());

        Assert.DoesNotContain("brand", body);
        Assert.DoesNotContain("child_platform", body);
    }

    [Fact]
    public async Task PostPaymentAsync_BrandWithOnlyName_OmitsNullUrls()
    {
        var payment = new PaymentBuilder().AddDestination("d").SetBrand("Acme").Build();

        var body = await PostBodyAsync(payment);

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("""{"name":"Acme"}""", doc.RootElement.GetProperty("brand").GetRawText());
    }

    [Fact]
    public async Task GetPaymentsAsync_BrandKey_PopulatesBrandAndMirrorsChildPlatform()
    {
        var client = CreateClient($$$"""[{"destinations": {{{Destinations}}}, "brand": {"name": "Acme", "logo_url": "{{{SameOrigin}}}/a.png"} }]""");

        var payment = Assert.Single(await client.GetPaymentsAsync("value"));

        Assert.Equal("Acme", payment.Brand!.Name);
        Assert.Equal($"{SameOrigin}/a.png", payment.Brand.LogoUrl);
        Assert.Equal("Acme", payment.ChildPlatform!.Name);
        Assert.Equal($"{SameOrigin}/a.png", payment.ChildPlatform.LogoUrl);
    }

    [Fact]
    public async Task GetPaymentsAsync_ChildPlatformKey_PopulatesBrandAndChildPlatform()
    {
        var client = CreateClient($$$"""[{"destinations": {{{Destinations}}}, "child_platform": {"name": "Acme", "logo_light_url": "{{{SameOrigin}}}/b.png"} }]""");

        var payment = Assert.Single(await client.GetPaymentsAsync("value"));

        Assert.Equal("Acme", payment.Brand!.Name);
        Assert.Equal($"{SameOrigin}/b.png", payment.Brand.LogoLightUrl);
        Assert.Equal("Acme", payment.ChildPlatform!.Name);
    }

    [Fact]
    public async Task GetPaymentsAsync_BothKeys_BrandWins()
    {
        var client = CreateClient($$$"""[{"destinations": {{{Destinations}}}, "child_platform": {"name": "Old"}, "brand": {"name": "New"} }]""");

        var payment = Assert.Single(await client.GetPaymentsAsync("value"));

        Assert.Equal("New", payment.Brand!.Name);
        Assert.Equal("New", payment.ChildPlatform!.Name);
    }

    [Fact]
    public async Task GetPaymentsAsync_NoBrandKeys_BothNull()
    {
        var client = CreateClient($$"""[{"destinations": {{Destinations}} }]""");

        var payment = Assert.Single(await client.GetPaymentsAsync("value"));

        Assert.Null(payment.Brand);
        Assert.Null(payment.ChildPlatform);
    }

    [Fact]
    public async Task GetPaymentsAsync_ChildPlatformKeyWithMismatchedLogo_Throws()
    {
        var json = $$"""[{"destinations": {{Destinations}}, "child_platform": {"logo_url": "{{OtherOrigin}}/evil.png"} }]""";
        var client = CreateClient(json);

        var ex = await Assert.ThrowsAsync<BrantaPaymentException>(() => client.GetPaymentsAsync("value"));
        Assert.Contains("brand.logoUrl", ex.Message);
    }
#pragma warning restore CS0618

    [Fact]
    public async Task GetPaymentsAsync_DoesNotSendSdkHeader()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler);

        await client.GetPaymentsAsync("value");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.False(request.Headers.Contains("X-Branta-SDK"));
    }

    [Fact]
    public async Task IsApiKeyValidAsync_DoesNotSendSdkHeader()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler);

        await client.IsApiKeyValidAsync(new BrantaApiOptions { ApiKey = "key" });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.False(request.Headers.Contains("X-Branta-SDK"));
    }
}
