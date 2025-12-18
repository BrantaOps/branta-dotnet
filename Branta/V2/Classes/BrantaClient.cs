using Branta.Classes;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
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
        var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, options);

        var response = await httpClient.GetAsync($"/v2/payments/{address}");

        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<Payment>>(cancellationToken) ?? [];
    }

    public async Task<List<Payment>> GetZKPaymentAsync(string address, string secret, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var payments = await GetPaymentsAsync(address, options, cancellationToken);

        foreach (var payment in payments)
        {
            foreach (var destination in payment.Destinations ?? [])
            {
                if (destination.IsZk == false) continue;

                destination.Value = AesEncryption.Decrypt(destination.Value, secret);
            }
        }

        return payments;
    }

    public async Task<Payment?> AddPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();
        ConfigureClient(httpClient, options);

        var apiKey = (options?.DefaultApiKey ?? _defaultOptions?.DefaultApiKey) ?? throw new Exception("Branta API Key is required to post payments.");

        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.PostAsJsonAsync("/v2/payments", payment, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new BrantaPaymentException(response.StatusCode.ToString());
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Payment>(responseBody, _jsonOptions);
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

        return (responsePayment, secret);
    }

    private void ConfigureClient(HttpClient httpClient, BrantaClientOptions? options)
    {
        var baseUrl = options?.BaseUrl ?? _defaultOptions?.BaseUrl ?? throw new Exception("Branta: BaseUrl is a required option.");
        httpClient.BaseAddress = new Uri(baseUrl.GetUrl());
    }
}
