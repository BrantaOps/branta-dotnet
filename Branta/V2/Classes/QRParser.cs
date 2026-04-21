using Branta.Enums;
using Branta.Exceptions;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Web;

namespace Branta.V2.Classes;

public partial class QRParser
{
    public string? Destination { get; set; }

    public DestinationType? DestinationType { get; set; }

    public string? EncryptedText { get; set; }

    public string? EncryptionSecret { get; set; }

    public QRParser(string qrText, Uri baseUri)
    {
        var text = qrText.Trim();

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            Destination = text;
            return;
        }

        if (uri.Scheme is "bitcoin" or "lightning")
        {
            DestinationType = GetDestinationType(text);
            Destination = GetDestination(text);

            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            EncryptedText = queryParams["branta_id"];
            EncryptionSecret = queryParams["branta_secret"];
            return;
        }

        if (uri.Scheme is "http" or "https")
        {
            if (!string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) || uri.Port != baseUri.Port)
            {
                throw new QRParseException("Invalid branta URL.");
            }

            Destination = EncryptedText = GetDestinationFromUrl(uri.AbsolutePath);

            var fragmentParams = HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#'));
            EncryptionSecret = fragmentParams["secret"];

            return;
        }

        Destination = text;
    }

    public bool IsZK()
    {
        return EncryptedText != null && EncryptionSecret != null;
    }

    private static string? GetDestination(string text)
    {
        var match = ColonToQuestionMarkRegex().Match(text);

        return match.Success ? match.Value : null;
    }

    private static DestinationType? GetDestinationType(string text)
    {
        if (text.StartsWith("bitcoin:", StringComparison.CurrentCultureIgnoreCase))
        {
            return Enums.DestinationType.BitcoinAddress;
        }
        else if (text.StartsWith("lightning:lnbc", StringComparison.CurrentCultureIgnoreCase) ||
            text.StartsWith("lightning:lntb", StringComparison.CurrentCultureIgnoreCase) ||
            text.StartsWith("lightning:lnbcrt", StringComparison.CurrentCultureIgnoreCase))
        {
            return Enums.DestinationType.Bolt11;
        }

        return null;
    }

    private static string? GetDestinationFromUrl(string url)
    {
        var match = DestinationFromUrlRegex().Match(url);

        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"(?<=:)[^?]*")]
    private static partial Regex ColonToQuestionMarkRegex();

    [GeneratedRegex(@"/v\d+/(?:verify|zk-verify)/([^?/]+)")]
    private static partial Regex DestinationFromUrlRegex();
}
