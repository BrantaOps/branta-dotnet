using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Classes;
using Branta.V2.Interfaces;
using Branta.V2.Models;
using Microsoft.Extensions.Options;

namespace Branta.V2.Services;

public class BrantaService(IBrantaClientNew client, IAesEncryption aesEncryption, IOptions<BrantaClientOptions> defaultOptions, ISecretGenerator? secretGenerator = null) : IBrantaService
{
    private readonly BrantaClientOptions _defaultOptions = defaultOptions.Value;
    private readonly ISecretGenerator _secretGenerator = secretGenerator ?? new GuidSecretGenerator();
    
    public Task<List<Payment>> GetPaymentsByQrCodeAsync(string qrText, BrantaClientOptions? options = null, CancellationToken ct = default)
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
            return Task.FromResult(new List<Payment>());

        return GetPaymentsAsync(destination, null, options, ct);
    }

    private async Task<List<Payment>> GetPaymentsForZkAsync(string lookupValue, string? encryptionKey, IReadOnlyList<string> additionalHashValues, BrantaClientOptions? options, CancellationToken ct)
    {
        var payments = await client.GetPaymentsAsync(lookupValue, options, ct);

        foreach (var payment in payments)
        {
            var keys = DecryptDestinations(payment.Destinations, lookupValue, encryptionKey, null);
            foreach (var value in additionalHashValues)
                DecryptHashZkDestinations(payment.Destinations, value, keys);
            payment.VerifyUrl = BuildVerifyUrl(options, lookupValue, keys);
        }

        return payments;
    }

    private void DecryptHashZkDestinations(List<Destination> destinations, string plainValue, Dictionary<string, string> keys)
    {
        var hashZkType = plainValue.GetHashZkType();
        if (!hashZkType.HasValue) return;

        var key = plainValue.ToNormalizedHash();
        foreach (var destination in destinations)
        {
            if (!destination.IsZk || destination.Type != hashZkType.Value) continue;
            destination.Value = aesEncryption.Decrypt(destination.Value, key);
            keys.TryAdd(destination.ZkId!, key);
        }
    }

    public async Task<List<Payment>> GetPaymentsAsync(string destinationValue, string? destinationEncryptionKey = null, BrantaClientOptions? options = null, CancellationToken ct = default)
    {
        var hashZkType = destinationValue.GetHashZkType();

        if (hashZkType == null && _defaultOptions.GetPrivacy(options) == PrivacyMode.Strict)
            throw new BrantaPaymentException("PrivacyMode.Strict does not permit plain-text lookups for this destination type.");

        var lookupValue = hashZkType.HasValue
            ? aesEncryption.Encrypt(destinationValue, destinationValue.ToNormalizedHash(), deterministicNonce: true)
            : destinationValue;

        var payments = await client.GetPaymentsAsync(lookupValue, options, ct);

        if (payments.Count == 0 && hashZkType.HasValue && _defaultOptions.GetPrivacy(options) != PrivacyMode.Strict)
        {
            lookupValue = destinationValue;
            payments = await client.GetPaymentsAsync(lookupValue, options, ct);
        }

        foreach (var payment in payments)
        {
            var destinationKeys = DecryptDestinations(payment.Destinations, destinationValue, destinationEncryptionKey, hashZkType);
            payment.VerifyUrl = BuildVerifyUrl(options, lookupValue, destinationKeys);
        }

        return payments;
    }

    private Dictionary<string, string> DecryptDestinations(List<Destination> destinations, string destinationValue, string? encryptionKey, DestinationType? hashZkType)
    {
        var keys = new Dictionary<string, string>();

        foreach (var destination in destinations)
        {
            if (!destination.IsZk) continue;

            if (destination.Type == DestinationType.BitcoinAddress)
            {
                if (encryptionKey == null) throw new Exception("Payment is ZK but no destination encryption key was provided.");

                destination.Value = aesEncryption.Decrypt(destination.Value, encryptionKey);
                keys.TryAdd(destination.ZkId!, encryptionKey);
            }
            else if (hashZkType.HasValue && destination.Type == hashZkType.Value)
            {
                var key = destinationValue.ToNormalizedHash();
                destination.Value = aesEncryption.Decrypt(destination.Value, key);
                keys.TryAdd(destination.ZkId!, key);
            }
        }

        return keys;
    }

    public async Task<(Payment, string)> AddPaymentAsync(Payment payment, BrantaClientOptions? options = null, CancellationToken ct = default)
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

                var key = destination.Value.ToNormalizedHash();
                destination.Value = aesEncryption.Encrypt(destination.Value, key, deterministicNonce: true);
                encryptedToKey[destination.Value] = key;
            }
        }

        var responsePayment = await client.PostPaymentAsync(payment, options, ct)
            ?? throw new BrantaPaymentException("No payment returned from server.");

        var keys = responsePayment.Destinations
            .Where(d => d.ZkId != null && encryptedToKey.ContainsKey(d.Value))
            .ToDictionary(d => d.ZkId!, d => encryptedToKey[d.Value]);

        var primaryValue = payment.Destinations.FirstOrDefault()?.Value ?? string.Empty;
        responsePayment.VerifyUrl = BuildVerifyUrl(options, primaryValue, keys);

        return (responsePayment, secret);
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
