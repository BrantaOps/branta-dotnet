using Branta.Enums;
using Branta.Extensions;
using System.Text.RegularExpressions;
using System.Web;

namespace Branta.V2.Classes;

public partial class QRParser
{
    public string? Destination { get; set; }

    public DestinationType? DestinationType { get; set; }

    public string? EncryptedText { get; set; }

    public string? EncryptionSecret { get; set; }

    public QRParser(string qrText)
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
            return Enums.DestinationType.BitcoinAddress;

        if (text.StartsWith("lightning:") && GetDestination(text)?.IsBolt11() == true)
            return Enums.DestinationType.Bolt11;

        return null;
    }

    [GeneratedRegex(@"(?<=:)[^?]*")]
    private static partial Regex ColonToQuestionMarkRegex();
}
