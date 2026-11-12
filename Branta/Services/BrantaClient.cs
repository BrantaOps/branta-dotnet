using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Branta;

internal class BrantaClient(IHttpClientFactory httpClientFactory, IOptions<BrantaApiOptions> brantaApiOptions) : IBrantaClient
{
    private readonly BrantaApiOptions? _defaultOptions = brantaApiOptions?.Value;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<List<Payment>> GetPaymentsAsync(string destinationValue, BrantaApiOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var baseUri = _defaultOptions.GetUri(options);
        var url = new Uri(baseUri, $"/v2/payments/{Uri.EscapeDataString(destinationValue)}");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new IncompleteError(ex.Message);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await DrainAsync(response, cancellationToken);
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                await DrainAsync(response, cancellationToken);
                throw new IncompleteError(response.StatusCode.ToString());
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrEmpty(body))
            {
                return [];
            }

            List<Payment> payments;
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    // Match JS/Kotlin/Swift: non-array 200 bodies decode as an empty list.
                    payments = [];
                }
                else
                {
                    payments = JsonSerializer.Deserialize<List<Payment>>(body, _jsonOptions) ?? [];
                }
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException or ArgumentException)
            {
                // Malformed JSON or a bad row shape — unfinished lookup, not a crash.
                throw new IncompleteError("MalformedResponse");
            }

            VerifyLogoUrls(baseUri, payments);

            return payments;
        }
    }

    public async Task<Payment?> PostPaymentAsync(Payment payment, BrantaApiOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var baseUri = _defaultOptions.GetUri(options);
        var apiKey = _defaultOptions.GetApiKey(options)
            ?? throw new BrantaPaymentException("Unauthorized");

        var json = JsonSerializer.Serialize(payment, _jsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/v2/payments"))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        SetHmacHeaders(request, json, baseUri, options);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await DrainAsync(response, cancellationToken);
            throw new BrantaPaymentException(response.StatusCode.ToString());
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (responseBody.Length == 0)
        {
            return null;
        }

        return JsonSerializer.Deserialize<Payment>(responseBody, _jsonOptions);
    }

    public async Task<bool> IsApiKeyValidAsync(BrantaApiOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var baseUri = _defaultOptions.GetUri(options);
        var apiKey = _defaultOptions.GetApiKey(options)
            ?? throw new BrantaPaymentException("Unauthorized");

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/v2/api-keys/health-check"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await DrainAsync(response, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private void SetHmacHeaders(HttpRequestMessage request, string json, Uri baseUri, BrantaApiOptions? options)
    {
        var hmacSecret = _defaultOptions.GetHmacSecret(options);
        if (hmacSecret == null) return;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var baseUrl = baseUri.ToString().TrimEnd('/');
        var message = $"POST|{baseUrl}/v2/payments|{json}|{timestamp}";

        var keyBytes = Encoding.UTF8.GetBytes(hmacSecret);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

        request.Headers.Add("X-HMAC-Signature", signature);
        request.Headers.Add("X-HMAC-Timestamp", timestamp);
    }

    private static void VerifyLogoUrls(Uri baseUri, List<Payment> payments)
    {
        var baseOrigin = baseUri.GetLeftPart(UriPartial.Authority);

        foreach (var payment in payments)
        {
            CheckLogoUrl(payment.PlatformLogoUrl, "platformLogoUrl", baseOrigin);
            CheckLogoUrl(payment.PlatformLogoLightUrl, "platformLogoLightUrl", baseOrigin);
            CheckLogoUrl(payment.ParentPlatform?.LogoUrl, "parentPlatform.logoUrl", baseOrigin);
            CheckLogoUrl(payment.ParentPlatform?.LogoLightUrl, "parentPlatform.logoLightUrl", baseOrigin);
            CheckLogoUrl(payment.ChildPlatform?.LogoUrl, "childPlatform.logoUrl", baseOrigin);
            CheckLogoUrl(payment.ChildPlatform?.LogoLightUrl, "childPlatform.logoLightUrl", baseOrigin);
        }
    }

    private static void CheckLogoUrl(string? logoUrl, string fieldName, string baseOrigin)
    {
        if (string.IsNullOrEmpty(logoUrl)) return;

        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var logoUri) ||
            logoUri.GetLeftPart(UriPartial.Authority) != baseOrigin)
        {
            throw new BrantaPaymentException($"{fieldName} domain does not match the configured API domain");
        }
    }

    private static async Task DrainAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content == null) return;
        try
        {
            await response.Content.CopyToAsync(Stream.Null, cancellationToken);
        }
        catch
        {
            // Best-effort drain so the connection can return to the pool.
        }
    }
}
