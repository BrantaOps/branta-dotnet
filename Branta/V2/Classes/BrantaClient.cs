using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Models;
using Microsoft.Extensions.Options;
using System.Net;
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
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<Payment>> GetPaymentsAsync(string address, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var privacy = options?.Privacy ?? _defaultOptions?.Privacy ?? PrivacyMode.Loose;
        if (privacy == PrivacyMode.Strict)
            throw new BrantaPaymentException("privacy is set to 'Strict': plain on-chain address lookups are not permitted");

        return await FetchPaymentsAsync(address, options, cancellationToken);
    }

    private async Task<List<Payment>> FetchPaymentsAsync(string address, BrantaClientOptions? options, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, options);

        var response = await httpClient.GetAsync($"/v2/payments/{Uri.EscapeDataString(address)}");

        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
        {
            return [];
        }

        var payments = await response.Content.ReadFromJsonAsync<List<Payment>>(cancellationToken) ?? [];

        // Validate that platformLogoUrl (if present) belongs to the configured base domain.
        var baseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        var baseOrigin = httpClient.BaseAddress?.GetLeftPart(UriPartial.Authority);
        if (baseOrigin != null)
        {
            foreach (var payment in payments)
            {
                var logoUrl = payment.PlatformLogoUrl;
                if (!string.IsNullOrEmpty(logoUrl))
                {
                    if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var logoUri) ||
                        logoUri.GetLeftPart(UriPartial.Authority) != baseOrigin)
                    {
                        throw new BrantaPaymentException("platformLogoUrl domain does not match the configured baseUrl domain");
                    }
                }
                payment.VerifyUrl = BuildVerifyUrl(baseUrl, address);
            }
        }

        return payments;
    }

    public async Task<List<Payment>> GetZKPaymentAsync(string address, string secret, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var payments = await FetchPaymentsAsync(address, options, cancellationToken);

        foreach (var payment in payments)
        {
            foreach (var destination in payment.Destinations ?? [])
            {
                if (destination.IsZk == false) continue;

                destination.Value = AesEncryption.Decrypt(destination.Value, secret);
            }
        }

        var baseUrl = (options?.BaseUrl ?? _defaultOptions?.BaseUrl)?.GetUrl() ?? "";
        foreach (var payment in payments)
        {
            payment.VerifyUrl = BuildVerifyUrl(baseUrl.TrimEnd('/'), address, secret);
        }

        return payments;
    }

    public async Task<Payment?> AddPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, options);
        SetApiKey(httpClient, options);

        var json = JsonSerializer.Serialize(payment);
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
            var baseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
            responsePayment.VerifyUrl = BuildVerifyUrl(baseUrl, payment.Destinations[0].Value);
        }
        return responsePayment;
    }

    public async Task<(Payment?, string)> AddZKPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var secret = Guid.NewGuid().ToString();

        foreach (var destination in payment.Destinations ?? [])
        {
            if (destination.IsZk == false) continue;

            destination.Value = AesEncryption.Encrypt(destination.Value, secret);
        }

        var responsePayment = await AddPaymentAsync(payment, options, cancellationToken);
        if (responsePayment != null && payment.Destinations?.Count > 0)
        {
            var baseUrl = (options?.BaseUrl ?? _defaultOptions?.BaseUrl)?.GetUrl()?.TrimEnd('/') ?? "";
            responsePayment.VerifyUrl = BuildVerifyUrl(baseUrl, payment.Destinations[0].Value, secret);
        }

        return (responsePayment, secret);
    }

    public async Task<List<Payment>> GetPaymentsByQrCodeAsync(string qrText, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var text = qrText.Trim();

        // ZK query params (branta_id + branta_secret) — always allowed regardless of privacy
        var queryStart = text.IndexOf('?');
        if (queryStart >= 0)
        {
            var rawQuery = text[(queryStart + 1)..];
            var fragIdx = rawQuery.IndexOf('#');
            if (fragIdx >= 0) rawQuery = rawQuery[..fragIdx];
            var qp = ParseQueryString(rawQuery);
            if (qp.TryGetValue("branta_id", out var bId) && qp.TryGetValue("branta_secret", out var bSec) && bId != null && bSec != null)
                return await GetZKPaymentAsync(bId, bSec, options, cancellationToken);
        }

        // Not a URI — treat as plain address
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
            return await GetPlainPaymentsAsync(NormalizeAddress(text), options, cancellationToken);

        // bitcoin: URI — strip optional query, then normalize
        if (uri.Scheme == "bitcoin")
        {
            var raw = text["bitcoin:".Length..];
            var qs = raw.IndexOf('?');
            var addr = qs >= 0 ? raw[..qs] : raw;
            return await GetPlainPaymentsAsync(NormalizeAddress("bitcoin:" + addr), options, cancellationToken);
        }

        // http/https URL matching the configured base URL
        if (uri.Scheme is "http" or "https")
        {
            var baseUrl = (options?.BaseUrl ?? _defaultOptions?.BaseUrl)?.GetUrl();
            if (baseUrl != null)
            {
                var baseUri = new Uri(baseUrl);
                if (string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) && uri.Port == baseUri.Port)
                {
                    var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length >= 3 && segments[0] == "v2")
                    {
                        var type = segments[1];
                        var id = Uri.UnescapeDataString(segments[2]);

                        if (type == "verify")
                            return await GetPlainPaymentsAsync(id, options, cancellationToken);

                        if (type == "zk-verify")
                        {
                            var fragment = uri.Fragment.TrimStart('#');
                            var fp = ParseQueryString(fragment);
                            fp.TryGetValue("secret", out var secret);
                            if (secret != null)
                                return await GetZKPaymentAsync(id, secret, options, cancellationToken);
                            return await GetPlainPaymentsAsync(id, options, cancellationToken);
                        }
                    }

                    // Fallback: use the last path segment
                    if (segments.Length > 0)
                    {
                        var last = Uri.UnescapeDataString(segments[^1]);
                        return await GetPlainPaymentsAsync(last, options, cancellationToken);
                    }
                }
            }
        }

        return await GetPlainPaymentsAsync(NormalizeAddress(text), options, cancellationToken);
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
        var baseUrl = options?.BaseUrl ?? _defaultOptions?.BaseUrl ?? throw new Exception("Branta: BaseUrl is a required option.");
        httpClient.BaseAddress = new Uri(baseUrl.GetUrl());
    }

    private void SetApiKey(HttpClient httpClient, BrantaClientOptions? options)
    {
        var apiKey = options?.DefaultApiKey ?? _defaultOptions?.DefaultApiKey;

        if (apiKey == null)
            throw new BrantaPaymentException("Unauthorized");

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

    private static string BuildVerifyUrl(string baseUrl, string address, string? secret = null)
    {
        var encoded = Uri.EscapeDataString(address);
        return secret != null
            ? $"{baseUrl}/v2/zk-verify/{encoded}#secret={secret}"
            : $"{baseUrl}/v2/verify/{encoded}";
    }

    private static string NormalizeAddress(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.StartsWith("lightning:")) return lower["lightning:".Length..];
        if (lower.StartsWith("bitcoin:"))
        {
            var addr = text["bitcoin:".Length..];
            var addrLower = addr.ToLowerInvariant();
            return addrLower.StartsWith("bc1q") || addrLower.StartsWith("bcrt") ? addrLower : addr;
        }
        if (lower.StartsWith("lnbc") || lower.StartsWith("bc1q")) return lower;
        return text;
    }

    private static Dictionary<string, string?> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx < 0)
            {
                result[Uri.UnescapeDataString(part)] = null;
            }
            else
            {
                var key = Uri.UnescapeDataString(part[..idx]);
                var val = Uri.UnescapeDataString(part[(idx + 1)..]);
                result[key] = val;
            }
        }
        return result;
    }
}
