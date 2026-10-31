using Branta.Enums;
using Branta.Extensions;
using System.Text.RegularExpressions;
using DestinationKind = Branta.Enums.DestinationType;

namespace Branta.V2.Classes;

public class QrDestination
{
    public required string Value { get; init; }
    public DestinationKind? Type { get; init; }
}

public class QRParser
{
    private static readonly Regex ColonToQuestion = new(@"(?<=:)[^?]*", RegexOptions.Compiled);

    public List<QrDestination> Destinations { get; } = [];
    public string? OnChainEncryptionText { get; private set; }
    public string? OnChainEncryptionSecret { get; private set; }
    public bool HasUnverifiableInstruction { get; private set; }

    public QRParser(string qrText)
    {
        var text = NormalizeQrText(qrText);

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
                Destinations.Add(new QrDestination { Value = dest, Type = destType });
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
        Destinations.Add(new QrDestination { Value = value, Type = type });
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
