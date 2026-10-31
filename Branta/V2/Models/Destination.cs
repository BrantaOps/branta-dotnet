using Branta.Enums;
using System.Text.Json.Serialization;

namespace Branta.V2.Models;

public class Destination
{
    public required string Value { get; set; }

    [JsonPropertyName("primary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("zk")]
    public bool Zk
    {
        get => true;
        set => IsEncrypted = value;
    }

    [JsonIgnore]
    public bool IsEncrypted { get; set; } = true;

    public DestinationType? Type { get; set; }

    [JsonPropertyName("zk_id")]
    public string? Id { get; set; }

    [JsonPropertyName("encrypted_dek")]
    public string? EncryptedDek { get; set; }
}
