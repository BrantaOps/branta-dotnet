using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Branta.V2.Classes;

public class BrantaClient(IHttpClientFactory httpClientFactory, IOptions<BrantaClientOptions> brantaClientOptions)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly BrantaClientOptions? _defaultOptions = brantaClientOptions?.Value;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<List<Payment>> GetPaymentsAsync(string address, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var privacy = options?.Privacy ?? _defaultOptions?.Privacy ?? PrivacyMode.Strict;
        if (privacy == PrivacyMode.Strict)
            throw new BrantaPaymentException("privacy is set to 'Strict': plain text lookups are not permitted");

        return await FetchPaymentsAsync(address, options, cancellationToken);
    }

    public async Task<List<Payment>> GetZKPaymentsAsync(string encryptedValue, string secret, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var payments = await FetchPaymentsAsync(encryptedValue, options, cancellationToken);

        var destinationKeys = new Dictionary<string, string>();

        foreach (var payment in payments)
        {
            foreach (var destination in payment.Destinations ?? [])
            {
                if (destination.IsZk == false) continue;

                if (destination.Type == DestinationType.BitcoinAddress || destination.Type == DestinationType.Bolt11)
                {
                    destination.Value = AesEncryption.Decrypt(destination.Value, secret);
                    destinationKeys.TryAdd(destination.ZkId!, secret);
                }
            }
        }

        var baseUrl = (options?.BaseUrl ?? _defaultOptions?.BaseUrl)?.GetUrl() ?? "";
        foreach (var payment in payments)
        {
            payment.VerifyUrl = BuildVerifyUrl(baseUrl.TrimEnd('/'), encryptedValue, destinationKeys);
        }

        return payments;
    }

    public async Task<List<Payment>> GetZKPaymentsWithHashSecretAsync(string plainTextValue, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var hash = plainTextValue.ToNormalizedHash();
        var encryptedValue = AesEncryption.Encrypt(plainTextValue, hash, deterministicNonce: true);

        var result = await GetZKPaymentsAsync(encryptedValue, hash, options, cancellationToken);

        if (result.Count > 0) return result;

        return await GetPaymentsAsync(plainTextValue, options, cancellationToken);
    }

    public async Task<Payment?> AddPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, options);
        SetApiKey(httpClient, options);

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

        var responsePayment = JsonSerializer.Deserialize<Payment>(responseBody, _jsonOptions);
        if (responsePayment != null && payment.Destinations.Count > 0)
        {
            var baseUrl = _defaultOptions.GetUri(options).AbsoluteUri?.TrimEnd('/') ?? "";
            responsePayment.VerifyUrl = BuildVerifyUrl(baseUrl, payment.Destinations[0].Value);
        }
        return responsePayment;
    }

    public async Task<(Payment?, string)> AddZKPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var secret = Guid.NewGuid().ToString();

        var destinationKeys = new Dictionary<string, string>();

        foreach (var destination in payment.Destinations ?? [])
        {
            if (destination.IsZk == false) continue;

            if (destination.Type == DestinationType.BitcoinAddress)
            {
                destination.Value = AesEncryption.Encrypt(destination.Value, secret);
                destinationKeys.TryAdd(destination.Value, secret);
            }
            else if (destination.Type == DestinationType.Bolt11)
            {
                var hash = destination.Value.ToNormalizedHash();
                destination.Value = AesEncryption.Encrypt(destination.Value, hash, deterministicNonce: true);
                destinationKeys.Add(destination.Value, hash);
            }
            else
            {
                throw new BrantaPaymentException($"destination type '{destination.Type}' does not support ZK");
            }
        }

        var responsePayment = await AddPaymentAsync(payment, options, cancellationToken);

        if (responsePayment != null && payment.Destinations?.Count > 0)
        {
            var keys = responsePayment.Destinations
                .Where(d => d.ZkId != null && destinationKeys.ContainsKey(d.Value))
                .ToDictionary(d => d.ZkId!, d => destinationKeys.GetValueOrDefault(d.Value)!);

            var baseUrl = _defaultOptions.GetUri(options).AbsoluteUri?.TrimEnd('/') ?? "";
            responsePayment.VerifyUrl = BuildVerifyUrl(baseUrl, payment.Destinations[0].Value, keys);
        }

        return (responsePayment, secret);
    }

    public async Task<List<Payment>> GetPaymentsByQrCodeAsync(string qrText, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var parser = new QRParser(qrText);

        if (parser.IsZK())
        {
            return await GetZKPaymentsAsync(parser.EncryptedText!, parser.EncryptionSecret!, options, cancellationToken);
        }
        else if (parser.DestinationType == DestinationType.Bolt11 && parser.Destination != null)
        {
            return await GetZKPaymentsWithHashSecretAsync(parser.Destination, options, cancellationToken);
        }
        else if (parser.Destination != null)
        {
            return await GetPlainPaymentsAsync(parser.Destination, options, cancellationToken);
        }

        return [];
    }
    
    private async Task<List<Payment>> FetchPaymentsAsync(string address, BrantaClientOptions? options, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, options);

        var response = await httpClient.GetAsync($"/v2/payments/{Uri.EscapeDataString(address)}", cancellationToken);

        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
        {
            return [];
        }

        var payments = await response.Content.ReadFromJsonAsync<List<Payment>>(_jsonOptions, cancellationToken) ?? [];

        VerifyLogoUrls(httpClient, payments);

        var baseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        foreach (var payment in payments)
        {
            payment.VerifyUrl = BuildVerifyUrl(baseUrl, address);
        }

        return payments;
    }

    private Task<List<Payment>> GetPlainPaymentsAsync(string address, BrantaClientOptions? options, CancellationToken cancellationToken)
    {
        var privacy = options?.Privacy ?? _defaultOptions?.Privacy ?? PrivacyMode.Loose;
        if (privacy == PrivacyMode.Strict)
            return Task.FromResult(new List<Payment>());
        return FetchPaymentsAsync(address, options, cancellationToken);
    }

    public async Task<bool> IsApiKeyValidAsync(BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, options);
        SetApiKey(httpClient, options);

        var response = await httpClient.GetAsync("/v2/api-keys/health-check", cancellationToken);

        return response.IsSuccessStatusCode;
    }

    private void ConfigureClient(HttpClient httpClient, BrantaClientOptions? options)
    {
        httpClient.BaseAddress = _defaultOptions.GetUri(options);
    }

    private void SetApiKey(HttpClient httpClient, BrantaClientOptions? options)
    {
        var apiKey = (options?.DefaultApiKey ?? _defaultOptions?.DefaultApiKey) ?? throw new BrantaPaymentException("Unauthorized");

        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    private void SetHmacHeaders(HttpRequestMessage request, string json, HttpClient httpClient, BrantaClientOptions? options)
    {
        var hmacSecret = options?.HmacSecret ?? _defaultOptions?.HmacSecret;
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

    private static string BuildVerifyUrl(string baseUrl, string address, Dictionary<string, string>? keys = null)
    {
        var encoded = Uri.EscapeDataString(address);
        var url = $"{baseUrl}/v2/verify/{encoded}";

        if (keys?.Count > 0)
        {
            var fragments = keys.Select(key => $"k-{key.Key}={key.Value}");
            url += "#" + string.Join("&", fragments);
        }

        return url;
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
