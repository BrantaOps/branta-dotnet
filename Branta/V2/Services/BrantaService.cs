using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Classes;
using Branta.V2.Interfaces;
using Branta.V2.Models;
using Microsoft.Extensions.Options;

namespace Branta.V2.Services;

public class BrantaService(IBrantaClient client, IAesEncryption aesEncryption, IOptions<BrantaClientOptions> defaultOptions, ISecretGenerator? secretGenerator = null) : IBrantaService
{
    private readonly BrantaClientOptions _defaultOptions = defaultOptions.Value;
    private readonly ISecretGenerator _secretGenerator = secretGenerator ?? new GuidSecretGenerator();

    public Task<PaymentsResult> GetPaymentsByQrCodeAsync(string qrText, BrantaClientOptions? options = null, CancellationToken ct = default)
    {
        var parser = new QRParser(qrText);

        if (parser.IsOnChainZk())
        {
            var additionalValues = parser.Destinations
                .Where(d => d.Value.GetHashZkType().HasValue)
                .Select(d => d.Value)
                .ToList();
            return GetPaymentsForZkAsync(parser.OnChainEncryptionText!, parser.OnChainEncryptionSecret, additionalValues, options, ct);
        }

        var destination = parser.Destination!;
        if (_defaultOptions.GetPrivacy(options) == PrivacyMode.Strict && destination.GetHashZkType() == null)
            return Task.FromResult(new PaymentsResult { Payments = [], VerifyUrl = BuildVerifyUrl(options, destination) });

        return GetPaymentsAsync(destination, null, options, ct);
    }

    private async Task<PaymentsResult> GetPaymentsForZkAsync(string lookupValue, string? encryptionKey, IReadOnlyList<string> additionalHashValues, BrantaClientOptions? options, CancellationToken ct)
    {
        var payments = await client.GetPaymentsAsync(lookupValue, options, ct);

        var keys = new Dictionary<string, string>();
        foreach (var payment in payments)
        {
            DecryptDestinations(payment.Destinations, lookupValue, encryptionKey, null, keys);
            foreach (var value in additionalHashValues)
                DecryptHashZkDestinations(payment.Destinations, value, keys);
        }

        return new PaymentsResult { Payments = payments, VerifyUrl = BuildVerifyUrl(options, lookupValue, keys) };
    }

    private void DecryptHashZkDestinations(List<Destination> destinations, string plainValue, Dictionary<string, string> keys)
    {
        var hashZkType = plainValue.GetHashZkType();
        if (!hashZkType.HasValue) return;

        var key = plainValue.ToNormalizedHash();
        foreach (var destination in destinations)
        {
            if (!destination.IsZk || destination.Type != hashZkType.Value) continue;
            try
            {
                destination.Value = aesEncryption.Decrypt(destination.Value, key);
                destination.IsEncrypted = false;
                keys.TryAdd(destination.ZkId!, key);
            }
            catch
            {
                // Key didn't match this destination — leave it encrypted.
            }
        }
    }

    public async Task<PaymentsResult> GetPaymentsAsync(string destinationValue, string? destinationEncryptionKey = null, BrantaClientOptions? options = null, CancellationToken ct = default)
    {
        var hashZkType = destinationValue.GetHashZkType();

        if (hashZkType == null && _defaultOptions.GetPrivacy(options) == PrivacyMode.Strict)
            throw new BrantaPaymentException("PrivacyMode.Strict does not permit plain-text lookups for this destination type.");

        var normalizedDestination = hashZkType.HasValue ? destinationValue.ToLowerInvariant() : destinationValue;
        var lookupValue = hashZkType.HasValue
            ? aesEncryption.Encrypt(normalizedDestination, normalizedDestination.ToNormalizedHash(), deterministicNonce: true)
            : destinationValue;

        var payments = await client.GetPaymentsAsync(lookupValue, options, ct);

        if (payments.Count == 0 && hashZkType.HasValue && _defaultOptions.GetPrivacy(options) != PrivacyMode.Strict)
        {
            lookupValue = normalizedDestination;
            payments = await client.GetPaymentsAsync(lookupValue, options, ct);
        }

        var keys = new Dictionary<string, string>();
        foreach (var payment in payments)
        {
            DecryptDestinations(payment.Destinations, normalizedDestination, destinationEncryptionKey, hashZkType, keys);
        }

        return new PaymentsResult { Payments = payments, VerifyUrl = BuildVerifyUrl(options, lookupValue, keys) };
    }

    private void DecryptDestinations(List<Destination> destinations, string destinationValue, string? encryptionKey, DestinationType? hashZkType, Dictionary<string, string> keys)
    {
        foreach (var destination in destinations)
        {
            destination.IsEncrypted = destination.IsZk;
            if (!destination.IsZk) continue;

            if (destination.Type == DestinationType.BitcoinAddress)
            {
                if (encryptionKey == null) continue;
                try
                {
                    destination.Value = aesEncryption.Decrypt(destination.Value, encryptionKey);
                    destination.IsEncrypted = false;
                    keys.TryAdd(destination.ZkId!, encryptionKey);
                }
                catch
                {
                    // Key didn't match this destination — leave it encrypted.
                }
            }
            else if (hashZkType.HasValue && destination.Type == hashZkType.Value)
            {
                var key = destinationValue.ToNormalizedHash();
                try
                {
                    destination.Value = aesEncryption.Decrypt(destination.Value, key);
                    destination.IsEncrypted = false;
                    keys.TryAdd(destination.ZkId!, key);
                }
                catch
                {
                    // Key didn't match this destination — leave it encrypted.
                }
            }
        }
    }

    public async Task<(Payment Payment, string Secret, string VerifyUrl)> AddPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken ct = default)
    {
        if (_defaultOptions.GetPrivacy(options) == PrivacyMode.Strict && payment.Destinations.Any(d => !d.IsZk))
            throw new BrantaPaymentException("PrivacyMode.Strict requires all destinations to be ZK; one or more destinations have IsZk = false.");

        var secret = _secretGenerator.Generate();
        var encryptedToKey = new Dictionary<string, string>();

        foreach (var destination in payment.Destinations)
        {
            if (!destination.IsZk) continue;

            if (destination.Type == DestinationType.BitcoinAddress)
            {
                destination.Value = aesEncryption.Encrypt(destination.Value, secret, _secretGenerator.DeterministicNonce);
                encryptedToKey[destination.Value] = secret;
            }
            else
            {
                var hashZkType = destination.Value.GetHashZkType();
                if (!hashZkType.HasValue)
                    throw new BrantaPaymentException($"destination type '{destination.Type}' does not support ZK");

                var normalizedValue = destination.Value.ToLowerInvariant();
                var key = normalizedValue.ToNormalizedHash();
                destination.Value = aesEncryption.Encrypt(normalizedValue, key, deterministicNonce: true);
                encryptedToKey[destination.Value] = key;
            }
        }

        var responsePayment = await client.PostPaymentAsync(payment, options, ct)
            ?? throw new BrantaPaymentException("No payment returned from server.");

        var keys = responsePayment.Destinations
            .Where(d => d.ZkId != null && encryptedToKey.ContainsKey(d.Value))
            .ToDictionary(d => d.ZkId!, d => encryptedToKey[d.Value]);

        var primaryValue = payment.Destinations.FirstOrDefault()?.Value ?? string.Empty;
        var verifyUrl = BuildVerifyUrl(options, primaryValue, keys);

        return (responsePayment, secret, verifyUrl);
    }

    public Task<bool> IsApiKeyValidAsync(BrantaClientOptions? options = null, CancellationToken ct = default)
    {
        return client.IsApiKeyValidAsync(options, ct);
    }

    private string BuildVerifyUrl(BrantaClientOptions? options, string paymentLookup, Dictionary<string, string>? keys = null)
    {
        var baseUrl = _defaultOptions.GetBaseUrl(options);
        var url = $"{baseUrl}/v2/verify/{Uri.EscapeDataString(paymentLookup)}";

        if (keys?.Count > 0)
        {
            url += keys.ToUrlFragment();
        }

        return url;
    }
}
