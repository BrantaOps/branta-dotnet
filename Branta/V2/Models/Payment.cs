using System.Text.Json.Serialization;

namespace Branta.V2.Models;

public class Payment
{
    public string? Description { get; set; }

    public required List<Destination> Destinations { get; set; } = [];

    [JsonPropertyName("created_at")]
    public DateTime CreatedDate { get; set; }

    public int TTL { get; set; }

    public string? Metadata { get; set; }

    public string? Platform { get; set; }

    public string? PlatformLogoUrl { get; set; }

    public string? PlatformLogoLightUrl { get; set; }

    public string? VerifyUrl { get; set; }

    public string? BtcPayServerPluginVersion { get; set; }
}
