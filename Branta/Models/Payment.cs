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

    /// <summary>
    /// The payee a shared-key platform tags the payment with. Sent on the wire as <c>brand</c>.
    /// Reads fall back to the deprecated <see cref="ChildPlatform"/> when no brand is set.
    /// </summary>
    public Platform? Brand
    {
        get => _brand ?? _childPlatform;
        set => _brand = value;
    }

    /// <summary>Deprecated name for <see cref="Brand"/>. Never sent on the wire. Reads return <see cref="Brand"/> when set.</summary>
    [Obsolete("Use Brand")]
    public Platform? ChildPlatform
    {
        get => _brand ?? _childPlatform;
        set => _childPlatform = value;
    }

    public string? BtcPayServerPluginVersion { get; set; }

    private Platform? _brand;

    private Platform? _childPlatform;

    [JsonIgnore]
    internal bool IsMetadataDecrypted { get; set; }
}
