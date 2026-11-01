using System.Text.Json.Serialization;

namespace Branta;

public class Destination
{
    public required string Value { get; set; }

    [JsonPropertyName("primary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("zk")]
    [JsonInclude]
    internal bool Zk
    {
        get => true;
        set => IsEncrypted = value;
    }

    [JsonIgnore]
    public bool IsEncrypted { get; set; } = true;

    [JsonConverter(typeof(NullableDestinationTypeConverter))]
    public DestinationType? Type { get; set; }

    [JsonPropertyName("zk_id")]
    public string? Id { get; set; }

    [JsonPropertyName("encrypted_dek")]
    public string? EncryptedDek { get; set; }
}
