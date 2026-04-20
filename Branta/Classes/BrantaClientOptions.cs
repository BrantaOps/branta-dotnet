using Branta.Enums;

namespace Branta.Classes;

public class BrantaClientOptions
{
    public required BrantaServerBaseUrl BaseUrl { get; set; }
    public string? DefaultApiKey { get; set; }
    public string? HmacSecret { get; set; }
    /// <inheritdoc cref="PrivacyMode"/>
    public required PrivacyMode Privacy { get; set; }
}
