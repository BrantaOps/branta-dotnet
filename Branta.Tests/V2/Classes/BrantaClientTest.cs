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
using System.Security.Cryptography;
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
            DefaultApiKey = "test-api-key",
            Privacy = PrivacyMode.Loose
        };
        _optionsMock.Setup(x => x.Value).Returns(_defaultOptions);
        _sut = new BrantaClient(_httpClientFactoryMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task GetPaymentAsync_ShouldReturnPayments()
    {
        var address = "test-address";
        var jsonResponse = JsonSerializer.Serialize(_testPayments, SnakeCaseOptions);
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
            BaseUrl = BrantaServerBaseUrl.Production,
            Privacy = PrivacyMode.Loose
        };
        var httpClient = SetupHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.GetPaymentsAsync(address, customOptions);

        Assert.Equal(new Uri(BrantaServerBaseUrl.Production.GetUrl()), httpClient.BaseAddress);
    }

    [Fact]
    public async Task GetPaymentsAsync_UrlEncodesAddress()
    {
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.GetPaymentsAsync("addr+with+plus");

        Assert.Single(capturedRequests);
        Assert.Equal("/v2/payments/addr%2Bwith%2Bplus", capturedRequests[0].RequestUri?.AbsolutePath + capturedRequests[0].RequestUri?.Query);
    }

    [Fact]
    public async Task GetPaymentsAsync_PlatformLogoUrlDomainMismatch_ThrowsException()
    {
        var payments = new List<Payment>
        {
            new() {
                Destinations = [new Destination { Value = "addr" }],
                PlatformLogoUrl = "https://evil.com/logo.png"
            }
        };
        var httpClient = SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(payments, SnakeCaseOptions));
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<BrantaPaymentException>(() => _sut.GetPaymentsAsync("addr"));
        Assert.Contains("platformLogoUrl domain does not match", exception.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_PlatformLogoUrlSameDomain_ReturnsPayments()
    {
        var baseUrl = BrantaServerBaseUrl.Localhost.GetUrl();
        var payments = new List<Payment>
        {
            new() {
                Destinations = [new Destination { Value = "addr" }],
                PlatformLogoUrl = $"{baseUrl}/logo.png"
            }
        };
        var httpClient = SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(payments, SnakeCaseOptions));
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.GetPaymentsAsync("addr");

        Assert.Single(result);
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
        var jsonResponse = JsonSerializer.Serialize(payments, SnakeCaseOptions);
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
        var jsonResponse = JsonSerializer.Serialize(payments, SnakeCaseOptions);
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
            DefaultApiKey = null,
            Privacy = PrivacyMode.Loose
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
            DefaultApiKey = "custom-api-key",
            Privacy = PrivacyMode.Loose
        };
        var jsonResponse = JsonSerializer.Serialize(_testPayments.First(), SnakeCaseOptions);
        var httpClient = SetupHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.AddPaymentAsync(payment, customOptions);

        Assert.Equal("Bearer", httpClient.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal("custom-api-key", httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }

    // ── GetPaymentsByQrCodeAsync ──────────────────────────────────────────────
    [Fact]
    public async Task GetPaymentsByQrCodeAsync_BrantaZkQueryParams_CallsGetZKPaymentWithId()
    {
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.GetPaymentsByQrCodeAsync("bitcoin:BC1Q?branta_id=payid&branta_secret=sec");

        Assert.Equal($"/v2/payments/payid", capturedRequests[0].RequestUri?.AbsolutePath);
    }

    [Theory]
    [InlineData("bitcoin:BC1QABC123", "BC1QABC123")]
    [InlineData("bitcoin:BCRT1QABC", "BCRT1QABC")]
    [InlineData("bitcoin:1ABCDef", "1ABCDef")]
    public async Task GetPaymentsByQrCodeAsync_BitcoinUri_NormalizesAddress(string qr, string expectedId)
    {
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.GetPaymentsByQrCodeAsync(qr);

        Assert.Equal($"/v2/payments/{expectedId}", capturedRequests[0].RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_WhitespaceTrimmed()
    {
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.GetPaymentsByQrCodeAsync("  some-payment-id  ");

        Assert.Equal("/v2/payments/some-payment-id", capturedRequests[0].RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_BitcoinUriBip21WithBrantaZkParams_CallsGetZKPayment()
    {
        const string encryptedId = "ENC+KEY==";
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.GetPaymentsByQrCodeAsync($"bitcoin:BC1QTEST?amount=0.001&branta_id={Uri.EscapeDataString(encryptedId)}&branta_secret=sec");

        Assert.Equal($"/v2/payments/{Uri.EscapeDataString(encryptedId)}", capturedRequests[0].RequestUri?.AbsolutePath);
    }

    // ── AddPaymentAsync HMAC ──────────────────────────────────────────────────

    [Fact]
    public async Task AddPaymentAsync_WithHmacSecret_IncludesHmacHeaders()
    {
        var payment = _testPayments.First();
        var customOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            HmacSecret = "test-hmac-secret",
            Privacy = PrivacyMode.Loose
        };
        var jsonResponse = JsonSerializer.Serialize(_testPayments.First(), SnakeCaseOptions);
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.AddPaymentAsync(payment, customOptions);

        Assert.Single(capturedRequests);
        Assert.True(capturedRequests[0].Headers.Contains("X-HMAC-Signature"));
        Assert.True(capturedRequests[0].Headers.Contains("X-HMAC-Timestamp"));
    }

    [Fact]
    public async Task AddPaymentAsync_WithoutHmacSecret_OmitsHmacHeaders()
    {
        var payment = _testPayments.First();
        var jsonResponse = JsonSerializer.Serialize(_testPayments.First(), SnakeCaseOptions);
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.AddPaymentAsync(payment);

        Assert.Single(capturedRequests);
        Assert.False(capturedRequests[0].Headers.Contains("X-HMAC-Signature"));
        Assert.False(capturedRequests[0].Headers.Contains("X-HMAC-Timestamp"));
    }

    [Fact]
    public async Task AddPaymentAsync_HmacSignature_Is64CharLowercaseHex()
    {
        var payment = _testPayments.First();
        var customOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            HmacSecret = "test-hmac-secret",
            Privacy = PrivacyMode.Loose
        };
        var jsonResponse = JsonSerializer.Serialize(_testPayments.First(), SnakeCaseOptions);
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.AddPaymentAsync(payment, customOptions);

        var signature = capturedRequests[0].Headers.GetValues("X-HMAC-Signature").First();
        Assert.Equal(64, signature.Length);
        Assert.Matches("^[a-f0-9]+$", signature);
    }

    [Fact]
    public async Task AddPaymentAsync_HmacTimestamp_IsRecentUnixEpochSeconds()
    {
        var payment = _testPayments.First();
        var customOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            HmacSecret = "test-hmac-secret",
            Privacy = PrivacyMode.Loose
        };
        var beforeSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var jsonResponse = JsonSerializer.Serialize(_testPayments.First(), SnakeCaseOptions);
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.AddPaymentAsync(payment, customOptions);

        var afterSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestamp = long.Parse(capturedRequests[0].Headers.GetValues("X-HMAC-Timestamp").First());
        Assert.InRange(timestamp, beforeSec, afterSec);
        Assert.Matches(@"^\d{10}$", timestamp.ToString());
    }

    [Fact]
    public async Task AddPaymentAsync_HmacSignature_MatchesExpectedComputation()
    {
        var payment = _testPayments.First();
        const string hmacSecret = "test-hmac-secret";
        var customOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Localhost,
            DefaultApiKey = "test-api-key",
            HmacSecret = hmacSecret,
            Privacy = PrivacyMode.Loose
        };
        var jsonResponse = JsonSerializer.Serialize(_testPayments.First(), SnakeCaseOptions);
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.AddPaymentAsync(payment, customOptions);

        var signature = capturedRequests[0].Headers.GetValues("X-HMAC-Signature").First();
        var timestamp = capturedRequests[0].Headers.GetValues("X-HMAC-Timestamp").First();
        var body = await capturedRequests[0].Content!.ReadAsStringAsync();
        var baseUrl = BrantaServerBaseUrl.Localhost.GetUrl();
        var message = $"POST|{baseUrl}/v2/payments|{body}|{timestamp}";
        var expectedSig = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(hmacSecret), Encoding.UTF8.GetBytes(message))
        ).ToLowerInvariant();

        Assert.Equal(expectedSig, signature);
    }

    [Fact]
    public async Task AddPaymentAsync_UsesHmacSecretFromDefaultOptions()
    {
        _defaultOptions.HmacSecret = "default-hmac-secret";
        var payment = _testPayments.First();
        var jsonResponse = JsonSerializer.Serialize(_testPayments.First(), SnakeCaseOptions);
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await _sut.AddPaymentAsync(payment);

        Assert.True(capturedRequests[0].Headers.Contains("X-HMAC-Signature"));

        _defaultOptions.HmacSecret = null;
    }

    // ── VerifyUrl population ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPaymentsAsync_SetsVerifyUrlOnReturnedPayments()
    {
        var address = "bc1qabc";
        var payments = new List<Payment> { new() { Destinations = [new Destination { Value = address }] } };
        var httpClient = SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(payments, SnakeCaseOptions));
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.GetPaymentsAsync(address);

        var baseUrl = BrantaServerBaseUrl.Localhost.GetUrl().TrimEnd('/');
        Assert.Equal($"{baseUrl}/v2/verify/{Uri.EscapeDataString(address)}", result[0].VerifyUrl);
    }

    [Fact]
    public async Task GetZKPaymentAsync_SetsZkVerifyUrlWithSecretFragment()
    {
        const string secret = "1234";
        var encryptedValue = "pQerSFV+fievHP+guYoGJjx1CzFFrYWHAgWrLhn5473Z19M6+WMScLd1hsk808AEF/x+GpZKmNacFBf5BbQ=";
        var payments = new List<Payment>
        {
            new() { Destinations = [new Destination { IsZk = true, Value = encryptedValue }] }
        };
        var httpClient = SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(payments, SnakeCaseOptions));
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.GetZKPaymentAsync(encryptedValue, secret);

        var baseUrl = BrantaServerBaseUrl.Localhost.GetUrl().TrimEnd('/');
        Assert.Equal($"{baseUrl}/v2/zk-verify/{Uri.EscapeDataString(encryptedValue)}#secret={secret}", result[0].VerifyUrl);
    }

    [Fact]
    public async Task AddPaymentAsync_SetsVerifyUrlOnReturnedPayment()
    {
        var payment = _testPayments.First();
        var jsonResponse = JsonSerializer.Serialize(payment, SnakeCaseOptions);
        var httpClient = SetupHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.AddPaymentAsync(payment);

        var baseUrl = BrantaServerBaseUrl.Localhost.GetUrl().TrimEnd('/');
        Assert.Equal($"{baseUrl}/v2/verify/{Uri.EscapeDataString(payment.Destinations[0].Value)}", result?.VerifyUrl);
    }

    [Fact]
    public async Task AddZKPaymentAsync_SetsZkVerifyUrlOnReturnedPayment()
    {
        var destination = new Destination { Value = "bc1qabc", IsZk = true, Type = DestinationType.BitcoinAddress };
        var payment = new Payment { Destinations = [destination] };
        var jsonResponse = JsonSerializer.Serialize(payment, SnakeCaseOptions);
        var (httpClient, capturedRequests) = SetupCapturingHttpClient(HttpStatusCode.OK, jsonResponse);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var (result, secrets) = await _sut.AddZKPaymentAsync(payment);

        var sentBody = await capturedRequests[0].Content!.ReadAsStringAsync();
        var sentPayment = JsonSerializer.Deserialize<Payment>(sentBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var encryptedAddress = sentPayment!.Destinations[0].Value;
        var baseUrl = BrantaServerBaseUrl.Localhost.GetUrl().TrimEnd('/');
        Assert.Equal($"{baseUrl}/v2/zk-verify/{Uri.EscapeDataString(encryptedAddress)}#secret={secrets[destination]}", result?.VerifyUrl);
    }

    [Fact]
    public async Task AddZKPaymentAsync_UnsupportedType_ThrowsBrantaPaymentException()
    {
        var payment = new Payment { Destinations = [new Destination { Value = "lnbc123", IsZk = true, Type = DestinationType.Bolt11 }] };

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _sut.AddZKPaymentAsync(payment));
    }

    // ── Privacy mode ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaymentsAsync_StrictPrivacy_ViaOptions_ThrowsException()
    {
        var strictOptions = new BrantaClientOptions { BaseUrl = BrantaServerBaseUrl.Localhost, Privacy = PrivacyMode.Strict };
        var httpClient = SetupHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<BrantaPaymentException>(() => _sut.GetPaymentsAsync("addr", strictOptions));
        Assert.Contains("Strict", exception.Message);
    }

    [Fact]
    public async Task GetPaymentsAsync_StrictPrivacy_ViaDefaultOptions_ThrowsException()
    {
        _defaultOptions.Privacy = PrivacyMode.Strict;
        var httpClient = SetupHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _sut.GetPaymentsAsync("addr"));

        _defaultOptions.Privacy = PrivacyMode.Loose;
    }

    [Fact]
    public async Task GetPaymentsAsync_LoosePrivacy_DoesNotThrow()
    {
        var looseOptions = new BrantaClientOptions { BaseUrl = BrantaServerBaseUrl.Localhost, Privacy = PrivacyMode.Loose };
        var httpClient = SetupHttpClient(HttpStatusCode.OK, "[]");
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _sut.GetPaymentsAsync("addr", looseOptions);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("plain-address")]
    [InlineData("bc1qabc123")]
    public async Task GetPaymentsByQrCodeAsync_StrictPrivacy_PlainAddress_ReturnsEmpty(string qr)
    {
        var strictOptions = new BrantaClientOptions { BaseUrl = BrantaServerBaseUrl.Localhost, Privacy = PrivacyMode.Strict };

        var result = await _sut.GetPaymentsByQrCodeAsync(qr, strictOptions);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("bitcoin:BC1QABC123")]
    [InlineData("bitcoin:1ABCDef")]
    public async Task GetPaymentsByQrCodeAsync_StrictPrivacy_BitcoinUri_ReturnsEmpty(string qr)
    {
        var strictOptions = new BrantaClientOptions { BaseUrl = BrantaServerBaseUrl.Localhost, Privacy = PrivacyMode.Strict };

        var result = await _sut.GetPaymentsByQrCodeAsync(qr, strictOptions);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_StrictPrivacy_VerifyUrl_ReturnsEmpty()
    {
        var strictOptions = new BrantaClientOptions { BaseUrl = BrantaServerBaseUrl.Localhost, Privacy = PrivacyMode.Strict };

        var result = await _sut.GetPaymentsByQrCodeAsync("http://localhost:3000/v2/verify/bc1qabc123", strictOptions);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPaymentsByQrCodeAsync_StrictPrivacy_ZkVerifyUrlWithoutSecret_ReturnsEmpty()
    {
        var strictOptions = new BrantaClientOptions { BaseUrl = BrantaServerBaseUrl.Localhost, Privacy = PrivacyMode.Strict };

        var result = await _sut.GetPaymentsByQrCodeAsync("http://localhost:3000/v2/zk-verify/ZK_ID", strictOptions);

        Assert.Empty(result);
    }

    // ── Snake_case serialization ──────────────────────────────────────────────

    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public void Payment_Serializes_To_SnakeCase_Keys()
    {
        var payment = new Payment
        {
            Destinations = [new Destination { Value = "addr" }],
            PlatformLogoUrl = "https://example.com/logo.png",
            PlatformLogoLightUrl = "https://example.com/logo-light.png",
            VerifyUrl = "https://example.com/verify/addr",
            BtcPayServerPluginVersion = "1.0"
        };

        var json = JsonSerializer.Serialize(payment, SnakeCaseOptions);

        Assert.Contains("\"platform_logo_url\"", json);
        Assert.Contains("\"platform_logo_light_url\"", json);
        Assert.Contains("\"verify_url\"", json);
        Assert.Contains("\"btc_pay_server_plugin_version\"", json);
    }

    [Fact]
    public void Payment_Deserializes_From_SnakeCase_Keys()
    {
        var json = """
            {
                "platform_logo_url": "https://example.com/logo.png",
                "platform_logo_light_url": "https://example.com/logo-light.png",
                "verify_url": "https://example.com/verify/addr",
                "btc_pay_server_plugin_version": "1.0",
                "destinations": [{ "value": "addr" }]
            }
            """;

        var payment = JsonSerializer.Deserialize<Payment>(json, SnakeCaseOptions);

        Assert.NotNull(payment);
        Assert.Equal("https://example.com/logo.png", payment.PlatformLogoUrl);
        Assert.Equal("https://example.com/logo-light.png", payment.PlatformLogoLightUrl);
        Assert.Equal("https://example.com/verify/addr", payment.VerifyUrl);
        Assert.Equal("1.0", payment.BtcPayServerPluginVersion);
    }

    [Fact]
    public void Destination_Serializes_To_SnakeCase_Keys()
    {
        var destination = new Destination
        {
            Value = "addr",
            IsPrimary = true,
            IsZk = true,
            Type = DestinationType.BitcoinAddress
        };

        var json = JsonSerializer.Serialize(destination, SnakeCaseOptions);

        Assert.Contains("\"primary\"", json);
        Assert.Contains("\"zk\"", json);
        Assert.Contains("\"value\"", json);
        Assert.Contains("\"type\"", json);
        Assert.DoesNotContain("\"is_primary\"", json);
        Assert.DoesNotContain("\"is_zk\"", json);
    }

    [Fact]
    public void Destination_Deserializes_From_SnakeCase_Keys()
    {
        var json = """
            {
                "value": "addr",
                "primary": true,
                "zk": true,
                "type": "bitcoin_address"
            }
            """;

        var destination = JsonSerializer.Deserialize<Destination>(json, SnakeCaseOptions);

        Assert.NotNull(destination);
        Assert.Equal("addr", destination.Value);
        Assert.True(destination.IsPrimary);
        Assert.True(destination.IsZk);
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

    private (HttpClient, List<HttpRequestMessage>) SetupCapturingHttpClient(HttpStatusCode statusCode, string? content)
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
