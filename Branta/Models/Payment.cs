using System.Text.Json.Serialization;

namespace Branta;

public class Payment
{
    [JsonIgnore]
    public string? Description { get; set; }

    public required List<Destination> Destinations { get; set; } = [];

    [JsonPropertyName("created_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Ttl { get; set; }

    public string? Metadata { get; set; }

    public string? Platform { get; set; }

    public string? PlatformLogoUrl { get; set; }

    public string? PlatformLogoLightUrl { get; set; }

    public Platform? ParentPlatform { get; set; }

    public Platform? ChildPlatform { get; set; }

    public string? BtcPayServerPluginVersion { get; set; }

    [JsonIgnore]
    internal bool IsMetadataDecrypted { get; set; }
}
