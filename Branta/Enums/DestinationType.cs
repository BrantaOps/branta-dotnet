using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Branta.Enums;

internal class DestinationTypeConverter : JsonConverter<DestinationType>
{
    public static DestinationType? TryParse(string? value)
    {
        if (value == null) return null;
        foreach (var field in typeof(DestinationType).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
            if ((attr?.Name ?? field.Name) == value)
                return (DestinationType)field.GetValue(null)!;
        }
        return null;
    }

    public static void WriteValue(Utf8JsonWriter writer, DestinationType value)
    {
        var field = typeof(DestinationType).GetField(value.ToString())!;
        var attr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
        writer.WriteStringValue(attr?.Name ?? value.ToString());
    }

    public override DestinationType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return TryParse(value) ?? throw new JsonException($"Unknown DestinationType: {value}");
    }

    public override void Write(Utf8JsonWriter writer, DestinationType value, JsonSerializerOptions options)
        => WriteValue(writer, value);
}

internal class NullableDestinationTypeConverter : JsonConverter<DestinationType?>
{
    public override DestinationType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return DestinationTypeConverter.TryParse(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, DestinationType? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        DestinationTypeConverter.WriteValue(writer, value.Value);
    }
}

[JsonConverter(typeof(DestinationTypeConverter))]
public enum DestinationType
{
    [JsonPropertyName("bitcoin_address")]
    BitcoinAddress,
    [JsonPropertyName("bolt11")]
    Bolt11,
    [JsonPropertyName("ark_address")]
    ArkAddress,
    [JsonPropertyName("silent_payment")]
    SilentPayment
}
