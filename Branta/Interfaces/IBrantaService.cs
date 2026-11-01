namespace Branta;

public interface IBrantaService
{
    Task<VerifyResult> VerifyAsync(string input, BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
    Task<AddPaymentResult> AddPaymentAsync(Payment payment, BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> IsApiKeyValidAsync(BrantaApiOptions? options = null, CancellationToken cancellationToken = default);
}
