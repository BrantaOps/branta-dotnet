using System.Text.Json.Serialization;

namespace Branta.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DestinationType
{
    [JsonPropertyName("bitcoin_address")]
    BitcoinAddress,
    [JsonPropertyName("bolt11")]
    Bolt11,
    [JsonPropertyName("bolt12")]
    Bolt12,
    [JsonPropertyName("ln_url")]
    LnUrl,
    [JsonPropertyName("tether_address")]
    TetherAddress
}
