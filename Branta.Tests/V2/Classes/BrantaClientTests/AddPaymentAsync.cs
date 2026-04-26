using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Moq;
using System.Security.Cryptography;
using System.Text;

namespace Branta.Tests.V2.Classes.BrantaClientTests;

public class AddPaymentAsync : DataBrantaClientTests
{
    [Fact]
    public async Task NoApiKey_ThrowsException()
    {
        _defaultOptions.DefaultApiKey = null;
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var exception = await Assert.ThrowsAsync<BrantaPaymentException>(() => _brantaClient.AddPaymentAsync(SingleBitcoinAddress));
        Assert.Equal("Unauthorized", exception.Message);
    }

    [Fact]
    public async Task UsesCustomApiKey()
    {
        var customOptions = new BrantaClientOptions
        {
            BaseUrl = BrantaServerBaseUrl.Production,
            DefaultApiKey = "custom-api-key",
            Privacy = PrivacyMode.Loose
        };
        var httpClient = SetupHttpClientForSinglePayment(SingleBitcoinAddress);

        await _brantaClient.AddPaymentAsync(SingleBitcoinAddress, customOptions);

        Assert.Equal("Bearer", httpClient.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal("custom-api-key", httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }

    [Fact]
    public async Task WithHmacSecret_IncludesHmacHeaders()
    {
        var (_, capturedRequests) = SetupCapturingHttpClientForSinglePayment(SingleBitcoinAddress);

        await _brantaClient.AddPaymentAsync(SingleBitcoinAddress, OptionsWithHmac());

        Assert.Single(capturedRequests);
        Assert.True(capturedRequests[0].Headers.Contains("X-HMAC-Signature"));
        Assert.True(capturedRequests[0].Headers.Contains("X-HMAC-Timestamp"));
    }

    [Fact]
    public async Task WithoutHmacSecret_OmitsHmacHeaders()
    {
        var (_, capturedRequests) = SetupCapturingHttpClientForSinglePayment(SingleBitcoinAddress);

        await _brantaClient.AddPaymentAsync(SingleBitcoinAddress);

        Assert.Single(capturedRequests);
        Assert.False(capturedRequests[0].Headers.Contains("X-HMAC-Signature"));
        Assert.False(capturedRequests[0].Headers.Contains("X-HMAC-Timestamp"));
    }

    [Fact]
    public async Task HmacSignature_Is64CharLowercaseHex()
    {
        var (_, capturedRequests) = SetupCapturingHttpClientForSinglePayment(SingleBitcoinAddress);

        await _brantaClient.AddPaymentAsync(SingleBitcoinAddress, OptionsWithHmac());

        var signature = capturedRequests[0].Headers.GetValues("X-HMAC-Signature").First();
        Assert.Equal(64, signature.Length);
        Assert.Matches("^[a-f0-9]+$", signature);
    }

    [Fact]
    public async Task HmacTimestamp_IsRecentUnixEpochSeconds()
    {
        var beforeSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var (_, capturedRequests) = SetupCapturingHttpClientForSinglePayment(SingleBitcoinAddress);

        await _brantaClient.AddPaymentAsync(SingleBitcoinAddress, OptionsWithHmac());

        var afterSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestamp = long.Parse(capturedRequests[0].Headers.GetValues("X-HMAC-Timestamp").First());
        Assert.InRange(timestamp, beforeSec, afterSec);
        Assert.Matches(@"^\d{10}$", timestamp.ToString());
    }

    [Fact]
    public async Task HmacSignature_MatchesExpectedComputation()
    {
        const string hmacSecret = "test-hmac-secret";
        var (_, capturedRequests) = SetupCapturingHttpClientForSinglePayment(SingleBitcoinAddress);

        await _brantaClient.AddPaymentAsync(SingleBitcoinAddress, OptionsWithHmac(hmacSecret));

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
    public async Task UsesHmacSecretFromDefaultOptions()
    {
        _defaultOptions.HmacSecret = "default-hmac-secret";
        var (_, capturedRequests) = SetupCapturingHttpClientForSinglePayment(SingleBitcoinAddress);

        await _brantaClient.AddPaymentAsync(SingleBitcoinAddress);

        Assert.True(capturedRequests[0].Headers.Contains("X-HMAC-Signature"));
    }

    [Fact]
    public async Task SetsVerifyUrlOnReturnedPayment()
    {
        SetupHttpClientForSinglePayment(SingleBitcoinAddress);

        var result = await _brantaClient.AddPaymentAsync(SingleBitcoinAddress);

        var baseUrl = BrantaServerBaseUrl.Localhost.GetUrl().TrimEnd('/');
        Assert.Equal($"{baseUrl}/v2/verify/{Uri.EscapeDataString(BITCOIN_ADDRESS)}", result?.VerifyUrl);
    }
}
