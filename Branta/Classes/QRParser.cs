using System.Globalization;
using System.Text.RegularExpressions;
using DestinationKind = Branta.DestinationType;

namespace Branta;

internal class QrDestination
{
    public required string Value { get; init; }

    /// <summary>
    /// Known rail type when the scheme or payload names one (e.g. bitcoin, or the token an ERC-681 chain id and contract name).
    /// </summary>
    public DestinationKind? Type { get; init; }

    /// <summary>True when this instruction is a secret-key address rail for verify.</summary>
    public bool SecretKey { get; init; }
}

internal class QRParser
{
    private static readonly Regex ColonToQuestion = new(@"(?<=:)[^?]*", RegexOptions.Compiled);

    public List<QrDestination> Destinations { get; } = [];
    public string? OnChainEncryptionText { get; private set; }
    public string? OnChainEncryptionSecret { get; private set; }

    /// <summary>
    /// Destination types this scan may decrypt. An ERC-681 scan sets exactly one type, from its chain id
    /// and token contract. A bitcoin scan sets <c>BitcoinAddress</c>.
    /// </summary>
    public IReadOnlyList<DestinationKind>? ExpectedSecretKeyTypes { get; private set; }

    public bool HasUnverifiableInstruction { get; private set; }

    public QRParser(string qrText)
    {
        var text = NormalizeQrText(qrText);

        if (text.StartsWith("ethereum:", StringComparison.OrdinalIgnoreCase))
        {
            var transfer = ParseErc681(text);
            if (transfer == null)
            {
                // Not a supported token transfer. Reads no branta params and cannot verify.
                Destinations.Add(new QrDestination { Value = text });
                return;
            }

            Destinations.Add(new QrDestination { Value = transfer.Value.Address, Type = transfer.Value.Type, SecretKey = true });
            ExpectedSecretKeyTypes = [transfer.Value.Type];

            var transferParams = ParseQueryString(ExtractQuery(text));
            if (transferParams.TryGetValue("branta_id", out var transferId))
                OnChainEncryptionText = transferId;
            if (transferParams.TryGetValue("branta_secret", out var transferSecret))
                OnChainEncryptionSecret = transferSecret;
            return;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            Destinations.Add(new QrDestination { Value = text, Type = DetectDestinationType(text) });
            return;
        }

        var scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is "bitcoin" or "lightning")
        {
            var dest = GetDestination(text);
            if (dest != null)
            {
                var destType = GetDestinationType(text);
                Destinations.Add(new QrDestination
                {
                    Value = dest,
                    Type = destType,
                    SecretKey = destType == DestinationKind.BitcoinAddress,
                });
                if (dest != "" && destType != DestinationKind.BitcoinAddress && dest.GetDerivedKeyType() == null)
                {
                    HasUnverifiableInstruction = true;
                }
            }

            var queryParams = ParseQueryString(uri.Query);
            if (queryParams.TryGetValue("branta_id", out var brantaId))
                OnChainEncryptionText = brantaId;
            if (queryParams.TryGetValue("branta_secret", out var brantaSecret))
                OnChainEncryptionSecret = brantaSecret;

            AddBindable(queryParams, "lightning", onChain: false);
            AddBindable(queryParams, "ark", onChain: false);
            AddBindable(queryParams, "silent_payment", onChain: false);
            if (queryParams.TryGetValue("bc", out var bc))
                AddBindableValue(bc, DestinationKind.BitcoinAddress, onChain: true);
            if (queryParams.TryGetValue("tb", out var tb))
                AddBindableValue(tb, DestinationKind.BitcoinAddress, onChain: true);
            AddBindable(queryParams, "sp", onChain: false);
            if (queryParams.ContainsKey("bolt12")) HasUnverifiableInstruction = true;
            if (queryParams.ContainsKey("lno")) HasUnverifiableInstruction = true;
            if (queryParams.ContainsKey("pay")) HasUnverifiableInstruction = true;

            if (Destinations.Any(d => d.SecretKey))
            {
                ExpectedSecretKeyTypes = [DestinationKind.BitcoinAddress];
            }

            return;
        }

        Destinations.Add(new QrDestination { Value = text });
    }

    public string? Destination => Destinations.Count > 0 ? Destinations[0].Value : null;

    public DestinationKind? DestinationType => Destinations.Count > 0 ? Destinations[0].Type : null;

    public bool HasBrantaParams() => OnChainEncryptionText != null && OnChainEncryptionSecret != null;

    public static DestinationKind? DetectDestinationType(string value)
    {
        if (value.IsBolt11()) return DestinationKind.Bolt11;
        if (value.IsArk()) return DestinationKind.ArkAddress;
        if (value.IsSilentPayment()) return DestinationKind.SilentPayment;
        if (value.StartsWith('1') ||
            value.StartsWith('3') ||
            value.StartsWith("bc1", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("tb1", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("bcrt1", StringComparison.OrdinalIgnoreCase))
        {
            return DestinationKind.BitcoinAddress;
        }
        return null;
    }

    private void AddBindable(Dictionary<string, string> queryParams, string key, bool onChain)
    {
        if (!queryParams.TryGetValue(key, out var value)) return;
        AddBindableValue(value, DetectDestinationType(value), onChain);
    }

    private void AddBindableValue(string value, DestinationKind? type, bool onChain)
    {
        if (value != "" && !onChain && value.GetDerivedKeyType() == null)
        {
            HasUnverifiableInstruction = true;
            return;
        }
        Destinations.Add(new QrDestination { Value = value, Type = type, SecretKey = onChain });
    }

    /// <summary>The query of a URI, read before any fragment. A "?" after a "#" belongs to the fragment.</summary>
    private static string ExtractQuery(string text)
    {
        var hash = text.IndexOf('#');
        if (hash >= 0) text = text[..hash];
        var after = text[(text.IndexOf(':') + 1)..];
        var question = after.IndexOf('?');
        return question < 0 ? "" : after[(question + 1)..];
    }

    /// <summary>Path of an <c>ethereum:</c> URI, without scheme, query, or fragment. One leading slash is dropped.</summary>
    private static string Erc681Path(string text)
    {
        var after = text[(text.IndexOf(':') + 1)..];
        var end = after.IndexOfAny(['?', '#']);
        var path = end < 0 ? after : after[..end];
        return path.StartsWith('/') ? path[1..] : path;
    }

    private static bool IsEvmAddress(string value) =>
        value.Length == 42
        && value[0] == '0'
        && (value[1] == 'x' || value[1] == 'X')
        && value.Skip(2).All(char.IsAsciiHexDigit);

    /// <summary>
    /// ERC-681 token transfer <c>ethereum:&lt;contract&gt;@&lt;chainId&gt;/transfer?address=&lt;deposit&gt;</c>.
    /// The chain id and contract select one destination type. <c>uint256</c> is not bound.
    /// Returns null for anything else: authority forms, other functions, ENS, hex or signed chain ids,
    /// an unknown rail, or a missing or malformed <c>address</c>.
    /// </summary>
    private static (DestinationKind Type, string Address)? ParseErc681(string text)
    {
        var path = Erc681Path(text);
        var at = path.IndexOf('@');
        if (at < 0) return null;

        var contract = path[..at];
        var rest = path[(at + 1)..];
        if (contract.Length != 42
            || !contract.StartsWith("0x", StringComparison.Ordinal)
            || !contract.Skip(2).All(char.IsAsciiHexDigit))
            return null;

        const string function = "/transfer";
        if (!rest.EndsWith(function, StringComparison.Ordinal)) return null;
        var chain = rest[..^function.Length];
        if (chain.Length == 0 || !chain.All(char.IsAsciiDigit)) return null;

        var digits = chain.TrimStart('0');
        // No supported chain id has more than 15 digits.
        if (digits.Length > 15) return null;
        var chainId = digits.Length == 0 ? 0 : long.Parse(digits, CultureInfo.InvariantCulture);

        var type = DestinationTypes.EvmTokenType(chainId, contract);
        if (type == null) return null;

        var query = ParseQueryString(ExtractQuery(text));
        if (!query.TryGetValue("address", out var address) || !IsEvmAddress(address)) return null;
        return (type.Value, address);
    }

    private static string? GetDestination(string text)
    {
        var match = ColonToQuestion.Match(text);
        return match.Success ? match.Value : null;
    }

    private static DestinationKind? GetDestinationType(string text)
    {
        if (text.StartsWith("bitcoin:", StringComparison.OrdinalIgnoreCase))
            return DestinationKind.BitcoinAddress;
        if (text.StartsWith("lightning:", StringComparison.OrdinalIgnoreCase))
        {
            var dest = GetDestination(text);
            if (dest != null && dest.IsBolt11()) return DestinationKind.Bolt11;
        }
        return null;
    }

    private static string NormalizeQrText(string qrText)
    {
        var value = qrText.Trim();
        var start = 0;
        var end = value.Length;
        while (start < end && value[start] <= 0x20) start++;
        while (end > start && value[end - 1] <= 0x20) end--;
        value = value[start..end];
        return value.Replace("\t", "").Replace("\n", "").Replace("\r", "");
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.StartsWith('?') ? query[1..] : query;
        if (trimmed.Length == 0) return result;

        foreach (var part in trimmed.Split('&'))
        {
            if (part.Length == 0) continue;
            var idx = part.IndexOf('=');
            var rawKey = idx == -1 ? part : part[..idx];
            var rawValue = idx == -1 ? "" : part[(idx + 1)..];
            if (rawKey.Length == 0) continue;
            try
            {
                var key = Uri.UnescapeDataString(rawKey).ToLowerInvariant();
                var value = Uri.UnescapeDataString(rawValue);
                result.TryAdd(key, value);
            }
            catch (UriFormatException)
            {
                // skip malformed parameter
            }
        }

        return result;
    }
}
