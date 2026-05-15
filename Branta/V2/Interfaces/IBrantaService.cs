using Branta.Classes;
using Branta.V2.Models;

namespace Branta.V2.Interfaces;

public interface IBrantaService
{
    Task<PaymentsResult> GetPaymentsAsync(string destinationValue, string? destinationEncryptionKey = null, BrantaClientOptions? options = null, CancellationToken ct = default);

    Task<PaymentsResult> GetPaymentsByQrCodeAsync(string qrText, BrantaClientOptions? options = null, CancellationToken ct = default);

    Task<(Payment Payment, string Secret, string VerifyUrl)> AddPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken ct = default);

    Task<bool> IsApiKeyValidAsync(BrantaClientOptions? options = null, CancellationToken ct = default);
}
