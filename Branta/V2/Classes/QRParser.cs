using Branta.Enums;
using Branta.Extensions;
using System.Text.RegularExpressions;
using System.Web;

namespace Branta.V2.Classes;

public record QrDestination(string Value, DestinationType? Type);

public partial class QRParser
{
    public List<QrDestination> Destinations { get; } = [];

    public string? Destination => Destinations.FirstOrDefault()?.Value;

    public DestinationType? DestinationType => Destinations.FirstOrDefault()?.Type;

    public string? OnChainEncryptionText { get; set; }

    public string? OnChainEncryptionSecret { get; set; }

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
                Destinations.Add(new QrDestination(dest, GetDestinationType(text)));

            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            OnChainEncryptionText = queryParams["branta_id"];
            OnChainEncryptionSecret = queryParams["branta_secret"];

            var lightningValue = queryParams["lightning"];
            if (lightningValue != null)
                Destinations.Add(new QrDestination(lightningValue, DetectPlainTextType(lightningValue)));

            var bolt12Value = queryParams["bolt12"];
            if (bolt12Value != null)
                Destinations.Add(new QrDestination(bolt12Value, DetectPlainTextType(bolt12Value)));

            var arkValue = queryParams["ark"];
            if (arkValue != null)
                Destinations.Add(new QrDestination(arkValue, DetectPlainTextType(arkValue)));

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
        if (value.StartsWith("ark1", StringComparison.OrdinalIgnoreCase)) return Enums.DestinationType.ArkAddress;
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

    [GeneratedRegex(@"(?<=:)[^?]*")]
    private static partial Regex ColonToQuestionMarkRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex LnAddressRegex();

    [GeneratedRegex(@"^[0-9a-fA-F]{40}$")]
    private static partial Regex EthereumHexRegex();
}
