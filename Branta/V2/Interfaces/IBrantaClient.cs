using Branta.Classes;
using Branta.V2.Models;

namespace Branta.V2.Interfaces;

public interface IBrantaClient
{
    Task<List<Payment>> GetPaymentsAsync(string destinationValue, BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
    Task<Payment?> PostPaymentAsync(Payment payment, BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> IsApiKeyValidAsync(BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
}
