using Branta.Classes;
using Branta.V2.Models;

namespace Branta.V2.Interfaces;

public interface IBrantaClientNew
{
    Task<List<Payment>> GetPaymentsAsync(string destinationValue, BrantaClientOptions? options = null, CancellationToken cancellationToken = default);
    Task<Payment?> PostPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> IsApiKeyValidAsync(BrantaClientOptions? options = null, CancellationToken cancellationToken = default);
}
