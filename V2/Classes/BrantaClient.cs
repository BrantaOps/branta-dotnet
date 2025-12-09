using Branta.V2.Models;
using System.Net.Http.Json;

namespace Branta.V2.Classes;

public class BrantaClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<List<Payment>> GetPaymentsAsync(string address)
    {
        var response = await _httpClient.GetAsync($"/v2/payments/{address}");

        if (!response.IsSuccessStatusCode || response?.Content == null)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<Payment>>() ?? [];
    }
}
