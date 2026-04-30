using Branta.Classes;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Interfaces;
using Branta.V2.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Branta.V2.Classes;

public class BrantaClient(IHttpClientFactory httpClientFactory, IOptions<BrantaClientOptions> brantaClientOptions) : IBrantaClientNew
{
    private readonly BrantaClientOptions? _defaultOptions = brantaClientOptions?.Value;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<List<Payment>> GetPaymentsAsync(string destinationValue, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = CreateConfiguredClient(options);

        var response = await httpClient.GetAsync($"/v2/payments/{Uri.EscapeDataString(destinationValue)}", cancellationToken);

        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
        {
            return [];
        }

        var payments = await response.Content.ReadFromJsonAsync<List<Payment>>(_jsonOptions, cancellationToken) ?? [];

        VerifyLogoUrls(httpClient, payments);

        return payments;
    }

    public async Task<Payment?> PostPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = CreateConfiguredClient(options, requireApiKey: true);

        var json = JsonSerializer.Serialize(payment, _jsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, "/v2/payments")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        SetHmacHeaders(request, json, httpClient, options);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new BrantaPaymentException(response.StatusCode.ToString());
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<Payment>(responseBody, _jsonOptions);
    }

    public async Task<bool> IsApiKeyValidAsync(BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = CreateConfiguredClient(options, requireApiKey: true);

        var response = await httpClient.GetAsync("/v2/api-keys/health-check", cancellationToken);

        return response.IsSuccessStatusCode;
    }

    private HttpClient CreateConfiguredClient(BrantaClientOptions? options, bool requireApiKey = false)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = _defaultOptions.GetUri(options);

        if (requireApiKey)
        {
            var apiKey = _defaultOptions.GetApiKey(options)
                ?? throw new BrantaPaymentException("Unauthorized");

            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        return httpClient;
    }

    private void SetHmacHeaders(HttpRequestMessage request, string json, HttpClient httpClient, BrantaClientOptions? options)
    {
        var hmacSecret = _defaultOptions.GetHmacSecret(options);
        if (hmacSecret == null) return;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var baseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        var message = $"POST|{baseUrl}/v2/payments|{json}|{timestamp}";

        var keyBytes = Encoding.UTF8.GetBytes(hmacSecret);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

        request.Headers.Add("X-HMAC-Signature", signature);
        request.Headers.Add("X-HMAC-Timestamp", timestamp);
    }

    private static void VerifyLogoUrls(HttpClient httpClient, List<Payment> payments)
    {
        var baseOrigin = httpClient.BaseAddress?.GetLeftPart(UriPartial.Authority);

        if (baseOrigin == null) return;

        foreach (var payment in payments)
        {
            var logoUrl = payment.PlatformLogoUrl;

            if (string.IsNullOrEmpty(logoUrl)) return;

            if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var logoUri) ||
                logoUri.GetLeftPart(UriPartial.Authority) != baseOrigin)
            {
                throw new BrantaPaymentException("platformLogoUrl domain does not match the configured baseUrl domain");
            }
        }
    }
}
