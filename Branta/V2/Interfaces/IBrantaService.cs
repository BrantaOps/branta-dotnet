using Branta.Classes;
using Branta.V2.Models;

namespace Branta.V2.Interfaces;

public interface IBrantaService
{
    Task<List<Payment>> GetPaymentsAsync(string destinationValue, string? destinationEncryptionKey = null, BrantaClientOptions? options = null, CancellationToken ct = default);

    Task<List<Payment>> GetPaymentsByQrCodeAsync(string qrText, BrantaClientOptions? options = null, CancellationToken ct = default);

    Task<(Payment, string)> AddPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken ct = default);

    Task<bool> IsApiKeyValidAsync(BrantaClientOptions? options = null, CancellationToken ct = default);
}