using Branta.Enums;
using Branta.Extensions;
using System.Text.RegularExpressions;

namespace Branta.V2.Classes;

public record QrDestination(string Value, DestinationType? Type);

public partial class QRParser
{
    public List<QrDestination> Destinations { get; } = [];

    public string? Destination => Destinations.FirstOrDefault()?.Value;

    public DestinationType? DestinationType => Destinations.FirstOrDefault()?.Type;

    public string? OnChainEncryptionText { get; set; }

    public string? OnChainEncryptionSecret { get; set; }

    /// <summary>A payable instruction this SDK cannot bind to a decrypted destination.</summary>
    public bool HasUnverifiableInstruction { get; private set; }

    public QRParser(string qrText)
    {
        var text = qrText.Trim();

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            Destinations.Add(new QrDestination(text, DetectPlainTextType(text)));
            return;
        }

        if (uri.Scheme is "bitcoin" or "lightning")
        {
            var dest = GetDestination(text);
            if (dest != null)
            {
                var destType = GetDestinationType(text);
                Destinations.Add(new QrDestination(dest, destType));
                if (dest.Length > 0 && destType != Enums.DestinationType.BitcoinAddress && dest.GetHashZkType() == null)
                    HasUnverifiableInstruction = true;
            }

            var queryParams = ParseQueryString(uri.Query);
            OnChainEncryptionText = queryParams.GetValueOrDefault("branta_id");
            OnChainEncryptionSecret = queryParams.GetValueOrDefault("branta_secret");

            void AddBindable(string? value, Enums.DestinationType? type, bool onChain)
            {
                if (value == null) return;
                Destinations.Add(new QrDestination(value, type));
                if (value.Length > 0 && !onChain && value.GetHashZkType() == null)
                    HasUnverifiableInstruction = true;
            }

            foreach (var key in new[] { "lightning", "bolt12", "ark", "silent_payment" })
            {
                var value = queryParams.GetValueOrDefault(key);
                AddBindable(value, value == null ? null : DetectPlainTextType(value), false);
            }

            AddBindable(queryParams.GetValueOrDefault("bc"), Enums.DestinationType.BitcoinAddress, true);
            AddBindable(queryParams.GetValueOrDefault("tb"), Enums.DestinationType.BitcoinAddress, true);

            var spValue = queryParams.GetValueOrDefault("sp");
            AddBindable(spValue, spValue == null ? null : DetectPlainTextType(spValue), false);

            var lnoValue = queryParams.GetValueOrDefault("lno");
            AddBindable(lnoValue, string.IsNullOrEmpty(lnoValue) ? null : DetectPlainTextType(lnoValue) ?? Enums.DestinationType.Bolt12, false);

            if (!string.IsNullOrEmpty(queryParams.GetValueOrDefault("pay")))
                HasUnverifiableInstruction = true;

            return;
        }

        Destinations.Add(new QrDestination(text, null));
    }

    public bool IsOnChainZk()
    {
        return OnChainEncryptionText != null && OnChainEncryptionSecret != null;
    }

    private static string? GetDestination(string text)
    {
        var match = ColonToQuestionMarkRegex().Match(text);
        return match.Success ? match.Value : null;
    }

    private static DestinationType? GetDestinationType(string text)
    {
        if (text.StartsWith("bitcoin:", StringComparison.OrdinalIgnoreCase))
            return Enums.DestinationType.BitcoinAddress;

        if (text.StartsWith("lightning:", StringComparison.OrdinalIgnoreCase))
        {
            var dest = GetDestination(text);
            if (dest?.IsBolt11() == true) return Enums.DestinationType.Bolt11;
            if (dest?.StartsWith("lno", StringComparison.OrdinalIgnoreCase) == true) return Enums.DestinationType.Bolt12;
            if (dest?.StartsWith("LNURL", StringComparison.OrdinalIgnoreCase) == true) return Enums.DestinationType.LnUrl;
        }

        return null;
    }

    private static DestinationType? DetectPlainTextType(string value)
    {
        if (value.IsBolt11()) return Enums.DestinationType.Bolt11;
        if (value.StartsWith("lno", StringComparison.OrdinalIgnoreCase)) return Enums.DestinationType.Bolt12;
        if (value.StartsWith("LNURL", StringComparison.OrdinalIgnoreCase)) return Enums.DestinationType.LnUrl;
        if (value.IsArk()) return Enums.DestinationType.ArkAddress;
        if (value.IsSilentPayment()) return Enums.DestinationType.SilentPayment;
        if (IsEthereumAddress(value)) return Enums.DestinationType.TetherAddress;
        if (IsTronAddress(value)) return Enums.DestinationType.TetherAddress;
        if (LnAddressRegex().IsMatch(value)) return Enums.DestinationType.LnAddress;
        if (value.StartsWith("1") || value.StartsWith("3") || value.StartsWith("bc1", StringComparison.OrdinalIgnoreCase))
            return Enums.DestinationType.BitcoinAddress;
        return null;
    }

    private static bool IsEthereumAddress(string value)
        => value.Length == 42 && value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && EthereumHexRegex().IsMatch(value[2..]);

    private static bool IsTronAddress(string value)
        => value.Length == 34 && value.StartsWith('T');

    private static Dictionary<string, string> ParseQueryString(string query)
        => query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p[0].Length > 0)
            .ToDictionary(
                p => Uri.UnescapeDataString(p[0]),
                p => p.Length > 1 ? Uri.UnescapeDataString(p[1]) : string.Empty,
                StringComparer.OrdinalIgnoreCase
            );

    [GeneratedRegex(@"(?<=:)[^?]*")]
    private static partial Regex ColonToQuestionMarkRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex LnAddressRegex();

    [GeneratedRegex(@"^[0-9a-fA-F]{40}$")]
    private static partial Regex EthereumHexRegex();
}
