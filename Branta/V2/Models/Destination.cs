using System.Text.Json.Serialization;

namespace Branta.V2.Models;

public class Destination
{
    public required string Value { get; set; }

    [JsonPropertyName("primary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("zk")]
    public bool IsZk { get; set; }
}
