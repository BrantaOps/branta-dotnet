using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Branta;

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
    SilentPayment,
    [JsonPropertyName("usdt_polygon")]
    UsdtPolygon,
    [JsonPropertyName("usdc_polygon")]
    UsdcPolygon
}

/// <summary>
/// Helpers for destination-type families used by verify and encrypt.
/// </summary>
public static class DestinationTypes
{
    /// <summary>
    /// Polygon EVM secret-key rails. A <c>pol:</c> URI maps to this family for verify
    /// (match-if-any). Token is still the destination type on the payment.
    /// </summary>
    public static readonly IReadOnlyList<DestinationType> PolygonEvm =
    [
        DestinationType.UsdtPolygon,
        DestinationType.UsdcPolygon,
    ];

    public static bool IsPolygonEvm(DestinationType type) =>
        type is DestinationType.UsdtPolygon or DestinationType.UsdcPolygon;

    /// <summary>Address rails encrypted with a random branta_secret (not a derived key).</summary>
    public static bool IsSecretKeyAddress(DestinationType type) =>
        type == DestinationType.BitcoinAddress || IsPolygonEvm(type);
}
