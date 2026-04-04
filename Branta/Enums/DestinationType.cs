using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Branta.Enums;

public class DestinationTypeConverter : JsonConverter<DestinationType>
{
    public override DestinationType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        foreach (var field in typeof(DestinationType).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
            if ((attr?.Name ?? field.Name) == value)
                return (DestinationType)field.GetValue(null)!;
        }
        throw new JsonException($"Unknown DestinationType: {value}");
    }

    public override void Write(Utf8JsonWriter writer, DestinationType value, JsonSerializerOptions options)
    {
        var field = typeof(DestinationType).GetField(value.ToString())!;
        var attr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
        writer.WriteStringValue(attr?.Name ?? value.ToString());
    }
}

[JsonConverter(typeof(DestinationTypeConverter))]
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
    TetherAddress,
    [JsonPropertyName("ln_address")]
    LnAddress,
    [JsonPropertyName("ark_address")]
    ArkAddress
}
