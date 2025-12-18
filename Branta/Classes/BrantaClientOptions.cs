using Branta.Enums;

namespace Branta.Classes;

public class BrantaClientOptions
{
    public required BrantaServerBaseUrl BaseUrl { get; set; }
    public string? DefaultApiKey { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
