using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Classes;
using Branta.V2.Interfaces;
using Branta.V2.Models;
using Microsoft.Extensions.Options;

namespace Branta.V2.Services;

public class BrantaService(
    IBrantaClient client,
    IAesEncryption aesEncryption,
    IOptions<BrantaApiOptions> defaultOptions,
    ISecretGenerator secretGenerator) : IBrantaService
{
    private readonly BrantaApiOptions _defaultOptions = defaultOptions.Value;

    public PaymentBuilder CreatePaymentBuilder() => new();

    public async Task<VerifyResult> VerifyAsync(string input, BrantaApiOptions? options = null, CancellationToken cancellationToken = default)
    {
        var parser = new QRParser(input);
        var lookup = parser.OnChainEncryptionText ?? parser.Destination ?? "";

        try
        {
            var expectedOnChainAddress = UniformOnChainAddress(parser);

            if (parser.HasUnverifiableInstruction)
            {
                return Unknown(options, lookup);
            }

            if (parser.HasBrantaParams())
            {
                var additionalValues = parser.Destinations
                    .Where(d => d.Value.GetDerivedKeyType() != null)
                    .Select(d => d.Value)
                    .ToList();
                if (expectedOnChainAddress == null && additionalValues.Count == 0)
                {
                    return Unknown(options, lookup);
                }

                var (payments, verifyUrl) = await LookupByBrantaId(
                    parser.OnChainEncryptionText!,
                    parser.OnChainEncryptionSecret,
                    additionalValues,
                    expectedOnChainAddress,
                    options,
                    cancellationToken);
                return ToVerifyResult(payments, verifyUrl);
            }

            var destination = parser.Destination!;
            if (destination.GetDerivedKeyType() == null)
            {
                return Unknown(options, destination);
            }

            var derived = await LookupByDerivedKey(destination, options, cancellationToken);
            return ToVerifyResult(derived.Payments, derived.VerifyUrl);
        }
        catch (TamperedError)
        {
            return new VerifyResult { Status = VerifyStatus.Tampered, VerifyUrl = BuildVerifyUrl(options, lookup) };
        }
        catch (IncompleteError)
        {
            return new VerifyResult { Status = VerifyStatus.Incomplete, VerifyUrl = BuildVerifyUrl(options, lookup) };
        }
    }

    public async Task<(Payment Payment, string Secret, string VerifyUrl)> AddPaymentAsync(
        Payment payment,
        BrantaApiOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        string? dek = null;
        if (payment.Metadata != null && payment.Destinations.Count > 0)
        {
            dek = secretGenerator.Generate();
            payment.Metadata = aesEncryption.Encrypt(payment.Metadata, dek, false);
        }

        var secret = secretGenerator.Generate();
        var encryptedToKey = new Dictionary<string, string>();

        foreach (var destination in payment.Destinations)
        {
            destination.Id ??= Guid.NewGuid().ToString();

            var type = destination.Type ?? QRParser.DetectDestinationType(destination.Value);
            if (destination.Type == null && type != null) destination.Type = type;

            if (type == DestinationType.BitcoinAddress)
            {
                destination.Value = aesEncryption.Encrypt(destination.Value, secret, secretGenerator.DeterministicNonce);
                encryptedToKey[destination.Value] = secret;
                if (dek != null)
                {
                    destination.EncryptedDek = aesEncryption.Encrypt(dek, secret, false);
                }
            }
            else
            {
                var derivedType = destination.Value.GetDerivedKeyType();
                if (derivedType == null)
                {
                    throw new BrantaPaymentException($"destination type '{destination.Type}' cannot be encrypted");
                }
                var normalizedValue = destination.Value.ToLowerInvariant();
                var key = normalizedValue.ToNormalizedHash();
                destination.Value = aesEncryption.Encrypt(normalizedValue, key, true);
                encryptedToKey[destination.Value] = key;
                if (dek != null)
                {
                    destination.EncryptedDek = aesEncryption.Encrypt(dek, key, false);
                }
            }
        }

        var responsePayment = await client.PostPaymentAsync(payment, options, cancellationToken)
            ?? throw new BrantaPaymentException("No payment returned from server.");

        var keys = new Dictionary<string, string>();
        foreach (var d in responsePayment.Destinations)
        {
            if (d.Id != null && encryptedToKey.TryGetValue(d.Value, out var key))
            {
                keys[d.Id] = key;
            }
        }

        var primaryValue = payment.Destinations.Count > 0 ? payment.Destinations[0].Value : "";
        var verifyUrl = BuildVerifyUrl(options, primaryValue, keys);

        return (responsePayment, secret, verifyUrl);
    }

    public Task<bool> IsApiKeyValidAsync(BrantaApiOptions? options = null, CancellationToken cancellationToken = default)
        => client.IsApiKeyValidAsync(options, cancellationToken);

    private async Task<(List<Payment> Payments, string VerifyUrl)> LookupByBrantaId(
        string lookupValue,
        string? encryptionKey,
        List<string> additionalHashValues,
        string? expectedOnChainAddress,
        BrantaApiOptions? options,
        CancellationToken cancellationToken)
    {
        var payments = await client.GetPaymentsAsync(lookupValue, options, cancellationToken);

        var keys = new Dictionary<string, string>();
        var bound = new List<Payment>();
        foreach (var payment in payments)
        {
            var onChainBound = DecryptDestinations(payment, encryptionKey, keys, expectedOnChainAddress);
            var hashesBound = true;
            foreach (var value in additionalHashValues)
            {
                if (!DecryptDerivedKeyDestinations(payment, value, keys)) hashesBound = false;
            }
            var hasPayableInstruction = expectedOnChainAddress != null || additionalHashValues.Count > 0;
            if (hasPayableInstruction && onChainBound && hashesBound) bound.Add(payment);
        }

        return (bound, BuildVerifyUrl(options, lookupValue, keys));
    }

    private async Task<(List<Payment> Payments, string VerifyUrl)> LookupByDerivedKey(
        string destinationValue,
        BrantaApiOptions? options,
        CancellationToken cancellationToken)
    {
        var normalizedDestination = destinationValue.ToLowerInvariant();
        var lookupValue = aesEncryption.Encrypt(
            normalizedDestination,
            normalizedDestination.ToNormalizedHash(),
            true);

        var payments = await client.GetPaymentsAsync(lookupValue, options, cancellationToken);

        var keys = new Dictionary<string, string>();
        var bound = new List<Payment>();
        foreach (var payment in payments)
        {
            if (DecryptDerivedKeyDestinations(payment, normalizedDestination, keys))
            {
                bound.Add(payment);
            }
        }

        return (bound, BuildVerifyUrl(options, lookupValue, keys));
    }

    private bool DecryptDerivedKeyDestinations(Payment payment, string plainValue, Dictionary<string, string> keys)
    {
        var derivedType = plainValue.GetDerivedKeyType();
        if (derivedType == null) return false;

        var key = plainValue.ToNormalizedHash();
        var bound = false;
        foreach (var destination in payment.Destinations)
        {
            if (!destination.IsEncrypted || destination.Type != derivedType) continue;
            try
            {
                destination.Value = aesEncryption.Decrypt(destination.Value, key);
                destination.IsEncrypted = false;
                if (destination.Id != null && !keys.ContainsKey(destination.Id))
                {
                    keys[destination.Id] = key;
                }
                TryDecryptMetadata(payment, destination, key);
                bound = true;
            }
            catch
            {
                // Key didn't match this destination — leave it encrypted.
            }
        }
        return bound;
    }

    private bool DecryptDestinations(
        Payment payment,
        string? encryptionKey,
        Dictionary<string, string> keys,
        string? expectedOnChainAddress)
    {
        var onChainBound = expectedOnChainAddress == null;
        foreach (var destination in payment.Destinations)
        {
            if (!destination.IsEncrypted) continue;
            destination.IsEncrypted = true;

            if (destination.Type != DestinationType.BitcoinAddress) continue;
            if (encryptionKey == null) continue;

            string decrypted;
            try
            {
                decrypted = aesEncryption.Decrypt(destination.Value, encryptionKey);
            }
            catch
            {
                continue;
            }

            if (expectedOnChainAddress != null && !AddressesMatch(decrypted, expectedOnChainAddress))
            {
                throw new TamperedError();
            }
            if (expectedOnChainAddress != null) onChainBound = true;
            destination.Value = decrypted;
            destination.IsEncrypted = false;
            if (destination.Id != null && !keys.ContainsKey(destination.Id))
            {
                keys[destination.Id] = encryptionKey;
            }
            TryDecryptMetadata(payment, destination, encryptionKey);
        }
        return onChainBound;
    }

    private void TryDecryptMetadata(Payment payment, Destination destination, string keyUsed)
    {
        if (destination.EncryptedDek == null || payment.Metadata == null || payment.IsMetadataDecrypted) return;
        try
        {
            var dek = aesEncryption.Decrypt(destination.EncryptedDek, keyUsed);
            payment.Metadata = aesEncryption.Decrypt(payment.Metadata, dek);
            payment.IsMetadataDecrypted = true;
        }
        catch
        {
            // DEK decryption failed — leave metadata as-is.
        }
    }

    private VerifyResult Unknown(BrantaApiOptions? options, string lookup)
        => new() { Status = VerifyStatus.Unknown, VerifyUrl = BuildVerifyUrl(options, lookup) };

    private static VerifyResult ToVerifyResult(List<Payment> payments, string verifyUrl)
        => payments.Count == 0
            ? new VerifyResult { Status = VerifyStatus.Unknown, VerifyUrl = verifyUrl }
            : new VerifyResult { Status = VerifyStatus.Match, Payments = payments, VerifyUrl = verifyUrl };

    private string BuildVerifyUrl(BrantaApiOptions? options, string paymentLookup, Dictionary<string, string>? keys = null)
    {
        var baseUrl = _defaultOptions.GetBaseUrl(options);
        var url = $"{baseUrl}/v2/verify/{Uri.EscapeDataString(paymentLookup)}";
        if (keys != null && keys.Count > 0)
        {
            url += keys.ToUrlFragment();
        }
        return url;
    }

    private static string? UniformOnChainAddress(QRParser parser)
    {
        var addresses = parser.Destinations
            .Where(d => d.Type == DestinationType.BitcoinAddress && d.Value.Trim() != "")
            .Select(d => d.Value)
            .ToList();
        if (addresses.Count == 0) return null;
        var first = addresses[0];
        foreach (var address in addresses.Skip(1))
        {
            if (!AddressesMatch(first, address))
            {
                throw new TamperedError();
            }
        }
        return first;
    }

    private static bool AddressesMatch(string a, string b)
    {
        static bool IsBech32(string v) =>
            v.StartsWith("bc1", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("tb1", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("bcrt1", StringComparison.OrdinalIgnoreCase);

        if (IsBech32(a) && IsBech32(b))
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        return a == b;
    }
}
