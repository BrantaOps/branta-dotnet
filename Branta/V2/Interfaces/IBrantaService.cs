using Branta.Classes;
using Branta.V2.Classes;
using Branta.V2.Models;

namespace Branta.V2.Interfaces;

public interface IBrantaService
{
    Task<VerifyResult> VerifyAsync(string input, BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
    Task<(Payment Payment, string Secret, string VerifyUrl)> AddPaymentAsync(Payment payment, BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> IsApiKeyValidAsync(BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
    PaymentBuilder CreatePaymentBuilder();
}
