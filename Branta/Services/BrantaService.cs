using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Branta;

public class BrantaService : IBrantaService
{
    private readonly IBrantaClient _client;
    private readonly IAesEncryption _aesEncryption;
    private readonly BrantaApiOptions _defaultOptions;
    private readonly ISecretGenerator _secretGenerator;

    public BrantaService(BrantaEnvironment environment, string? apiKey = null)
        : this(new BrantaApiOptions { Environment = environment, ApiKey = apiKey })
    {
    }

    public BrantaService(BrantaApiOptions options)
        : this(options, new DefaultHttpClientFactory())
    {
    }

    public BrantaService(BrantaApiOptions options, IHttpClientFactory httpClientFactory)
        : this(
            new BrantaClient(httpClientFactory, Options.Create(options)),
            new AesEncryption(),
            Options.Create(options),
            new GuidSecretGenerator())
    {
    }

    internal BrantaService(
        IBrantaClient client,
        IAesEncryption aesEncryption,
        IOptions<BrantaApiOptions> defaultOptions,
        ISecretGenerator secretGenerator)
    {
        _client = client;
        _aesEncryption = aesEncryption;
        _defaultOptions = defaultOptions.Value;
        _secretGenerator = secretGenerator;
    }

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
                    ExpectedSecretKeyTypes(parser),
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

    public async Task<AddPaymentResult> AddPaymentAsync(
        Payment payment,
        BrantaApiOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Encrypt and post this copy. A failed post must leave the caller's Payment reusable.
        var submitted = CopyPayment(payment);
        FoldDescriptionIntoMetadata(submitted);
        string? dek = null;
        if (submitted.Metadata != null && submitted.Destinations.Count > 0)
        {
            dek = _secretGenerator.Generate();
            submitted.Metadata = _aesEncryption.Encrypt(submitted.Metadata, dek, false);
        }

        var secret = _secretGenerator.Generate();
        var encryptedToKey = new Dictionary<string, string>();

        foreach (var destination in submitted.Destinations)
        {
            destination.Id ??= Guid.NewGuid().ToString();

            var type = destination.Type ?? QRParser.DetectDestinationType(destination.Value);
            if (destination.Type == null && type != null) destination.Type = type;

            if (type != null && DestinationTypes.IsSecretKeyAddress(type.Value))
            {
                destination.Value = _aesEncryption.Encrypt(destination.Value, secret, _secretGenerator.DeterministicNonce);
                encryptedToKey[destination.Value] = secret;
                if (dek != null)
                {
                    destination.EncryptedDek = _aesEncryption.Encrypt(dek, secret, false);
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
                destination.Value = _aesEncryption.Encrypt(normalizedValue, key, true);
                encryptedToKey[destination.Value] = key;
                if (dek != null)
                {
                    destination.EncryptedDek = _aesEncryption.Encrypt(dek, key, false);
                }
            }
        }

        var responsePayment = await _client.PostPaymentAsync(submitted, options, cancellationToken)
            ?? throw new BrantaPaymentException("No payment returned from server.");

        var keys = new Dictionary<string, string>();
        foreach (var d in responsePayment.Destinations)
        {
            if (d.Id != null && encryptedToKey.TryGetValue(d.Value, out var key))
            {
                keys[d.Id] = key;
            }
        }

        var primaryValue = submitted.Destinations.Count > 0 ? submitted.Destinations[0].Value : "";
        var verifyUrl = BuildVerifyUrl(options, primaryValue, keys);

        return new AddPaymentResult(responsePayment, secret, verifyUrl);
    }

    private static Payment CopyPayment(Payment payment) => new()
    {
        Description = payment.Description,
        Destinations = payment.Destinations.Select(CopyDestination).ToList(),
        CreatedAt = payment.CreatedAt,
        Ttl = payment.Ttl,
        Metadata = payment.Metadata,
        Platform = payment.Platform,
        PlatformLogoUrl = payment.PlatformLogoUrl,
        PlatformLogoLightUrl = payment.PlatformLogoLightUrl,
        ParentPlatform = CopyPlatform(payment.ParentPlatform),
        ChildPlatform = CopyPlatform(payment.ChildPlatform),
        BtcPayServerPluginVersion = payment.BtcPayServerPluginVersion,
        IsMetadataDecrypted = payment.IsMetadataDecrypted,
    };

    private static Destination CopyDestination(Destination destination) => new()
    {
        Value = destination.Value,
        IsPrimary = destination.IsPrimary,
        IsEncrypted = destination.IsEncrypted,
        Type = destination.Type,
        Id = destination.Id,
        EncryptedDek = destination.EncryptedDek,
    };

    private static Platform? CopyPlatform(Platform? platform) => platform == null
        ? null
        : new Platform
        {
            Name = platform.Name,
            LogoUrl = platform.LogoUrl,
            LogoLightUrl = platform.LogoLightUrl,
        };

    public Task<bool> IsApiKeyValidAsync(BrantaApiOptions? options = null, CancellationToken cancellationToken = default)
        => _client.IsApiKeyValidAsync(options, cancellationToken);

    private async Task<(List<Payment> Payments, string VerifyUrl)> LookupByBrantaId(
        string lookupValue,
        string? encryptionKey,
        List<string> additionalHashValues,
        string? expectedOnChainAddress,
        HashSet<DestinationType>? expectedOnChainTypes,
        BrantaApiOptions? options,
        CancellationToken cancellationToken)
    {
        var payments = await _client.GetPaymentsAsync(lookupValue, options, cancellationToken);

        var keys = new Dictionary<string, string>();
        var bound = new List<Payment>();
        foreach (var payment in payments)
        {
            var onChainBound = DecryptDestinations(
                payment,
                encryptionKey,
                keys,
                expectedOnChainAddress,
                expectedOnChainTypes);
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
        var lookupValue = _aesEncryption.Encrypt(
            normalizedDestination,
            normalizedDestination.ToNormalizedHash(),
            true);

        var payments = await _client.GetPaymentsAsync(lookupValue, options, cancellationToken);

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
            string decrypted;
            try
            {
                decrypted = _aesEncryption.Decrypt(destination.Value, key);
            }
            catch
            {
                // Key didn't match this destination — leave it encrypted.
                continue;
            }

            if (!string.Equals(decrypted, plainValue, StringComparison.OrdinalIgnoreCase))
            {
                throw new TamperedError();
            }

            destination.Value = decrypted;
            destination.IsEncrypted = false;
            if (destination.Id != null && !keys.ContainsKey(destination.Id))
            {
                keys[destination.Id] = key;
            }
            TryDecryptMetadata(payment, destination, key);
            bound = true;
        }
        return bound;
    }

    private bool DecryptDestinations(
        Payment payment,
        string? encryptionKey,
        Dictionary<string, string> keys,
        string? expectedOnChainAddress,
        HashSet<DestinationType>? expectedOnChainTypes)
    {
        var onChainBound = expectedOnChainAddress == null;
        var decryptedExpectedRail = false;
        foreach (var destination in payment.Destinations)
        {
            if (!destination.IsEncrypted) continue;
            destination.IsEncrypted = true;

            if (destination.Type == null || !DestinationTypes.IsSecretKeyAddress(destination.Type.Value))
                continue;
            // Outside this QR's rail family (e.g. bitcoin when scanning pol:) — leave encrypted.
            if (expectedOnChainTypes != null && !expectedOnChainTypes.Contains(destination.Type.Value))
                continue;
            if (encryptionKey == null) continue;

            string decrypted;
            try
            {
                decrypted = _aesEncryption.Decrypt(destination.Value, encryptionKey);
            }
            catch
            {
                // Key didn't match this destination — leave it encrypted.
                continue;
            }

            decryptedExpectedRail = true;
            // Match-if-any within the family: a sibling rail with a different address stays encrypted.
            if (expectedOnChainAddress != null && !AddressesMatch(decrypted, expectedOnChainAddress))
            {
                continue;
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
        // Tampered only when a family rail decrypted and none matched the scanned address.
        if (expectedOnChainAddress != null && !onChainBound && decryptedExpectedRail)
        {
            throw new TamperedError();
        }
        return onChainBound;
    }

    private void TryDecryptMetadata(Payment payment, Destination destination, string keyUsed)
    {
        if (destination.EncryptedDek == null || payment.Metadata == null || payment.IsMetadataDecrypted) return;
        try
        {
            var dek = _aesEncryption.Decrypt(destination.EncryptedDek, keyUsed);
            payment.Metadata = _aesEncryption.Decrypt(payment.Metadata, dek);
            payment.IsMetadataDecrypted = true;
            LiftDescriptionFromMetadata(payment);
        }
        catch
        {
            // DEK decryption failed — leave metadata as-is.
        }
    }

    private static void FoldDescriptionIntoMetadata(Payment payment)
    {
        if (string.IsNullOrEmpty(payment.Description)) return;

        Dictionary<string, object> metadataMap;
        if (string.IsNullOrEmpty(payment.Metadata))
        {
            metadataMap = [];
        }
        else
        {
            try
            {
                metadataMap = JsonSerializer.Deserialize<Dictionary<string, object>>(payment.Metadata) ?? [];
            }
            catch (JsonException)
            {
                metadataMap = [];
            }
        }

        if (!metadataMap.ContainsKey("description"))
        {
            metadataMap["description"] = payment.Description;
        }

        payment.Metadata = JsonSerializer.Serialize(metadataMap);
        payment.Description = null;
    }

    private static void LiftDescriptionFromMetadata(Payment payment)
    {
        if (string.IsNullOrEmpty(payment.Metadata)) return;
        try
        {
            using var doc = JsonDocument.Parse(payment.Metadata);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("description", out var desc)
                && desc.ValueKind == JsonValueKind.String)
            {
                payment.Description = desc.GetString();
            }
        }
        catch (JsonException)
        {
            // opaque ciphertext or non-JSON
        }
    }

    private VerifyResult Unknown(BrantaApiOptions? options, string lookup)
        => new() { Status = VerifyStatus.Unknown, VerifyUrl = BuildVerifyUrl(options, lookup) };

    private static IReadOnlyList<Destination> MatchedDestinationsFrom(List<Payment> payments) =>
        payments.SelectMany(payment => payment.Destinations.Where(d => !d.IsEncrypted)).ToList();

    private static VerifyResult ToVerifyResult(List<Payment> payments, string verifyUrl)
        => payments.Count == 0
            ? new VerifyResult { Status = VerifyStatus.Unknown, VerifyUrl = verifyUrl }
            : new VerifyResult
            {
                Status = VerifyStatus.Match,
                Payments = payments,
                MatchedDestinations = MatchedDestinationsFrom(payments),
                VerifyUrl = verifyUrl,
            };

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

    private static IEnumerable<QrDestination> SecretKeyDestinations(QRParser parser) =>
        parser.Destinations.Where(d => d.SecretKey && d.Value.Trim() != "");

    private static string? UniformOnChainAddress(QRParser parser)
    {
        var addresses = SecretKeyDestinations(parser).Select(d => d.Value).ToList();
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

    private static HashSet<DestinationType>? ExpectedSecretKeyTypes(QRParser parser)
    {
        if (parser.ExpectedSecretKeyTypes == null || parser.ExpectedSecretKeyTypes.Count == 0)
            return null;
        return parser.ExpectedSecretKeyTypes.ToHashSet();
    }

    private static bool AddressesMatch(string a, string b)
    {
        static bool IsBech32(string v) =>
            v.StartsWith("bc1", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("tb1", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("bcrt1", StringComparison.OrdinalIgnoreCase);

        if (IsBech32(a) && IsBech32(b))
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        if (a.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
            b.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        return a == b;
    }

    private sealed class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
