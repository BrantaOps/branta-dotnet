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
    UsdcPolygon,
    [JsonPropertyName("usdt_ethereum")]
    UsdtEthereum,
    [JsonPropertyName("usdc_ethereum")]
    UsdcEthereum,
    [JsonPropertyName("usdt_bsc")]
    UsdtBsc,
    [JsonPropertyName("usdc_bsc")]
    UsdcBsc,
    [JsonPropertyName("usdt_tron")]
    UsdtTron,
    [JsonPropertyName("usdt_ton")]
    UsdtTon,
    [JsonPropertyName("usdt_solana")]
    UsdtSolana,
    [JsonPropertyName("usdc_solana")]
    UsdcSolana
}

/// <summary>
/// Internal helpers for destination-type families used by verify and encrypt.
/// </summary>
internal static class DestinationTypes
{
    /// <summary>
    /// Polygon EVM secret-key rails. A <c>pol:</c> URI maps to this family for verify
    /// (match-if-any). Token is still the destination type on the payment.
    /// </summary>
    internal static readonly IReadOnlyList<DestinationType> PolygonEvm =
    [
        DestinationType.UsdtPolygon,
        DestinationType.UsdcPolygon,
    ];

    internal static readonly IReadOnlyList<DestinationType> EthereumEvm =
    [
        DestinationType.UsdtEthereum,
        DestinationType.UsdcEthereum,
    ];

    internal static readonly IReadOnlyList<DestinationType> BscEvm =
    [
        DestinationType.UsdtBsc,
        DestinationType.UsdcBsc,
    ];

    internal static readonly IReadOnlyList<DestinationType> Tron =
    [
        DestinationType.UsdtTron,
    ];

    internal static readonly IReadOnlyList<DestinationType> Ton =
    [
        DestinationType.UsdtTon,
    ];

    internal static readonly IReadOnlyList<DestinationType> Solana =
    [
        DestinationType.UsdtSolana,
        DestinationType.UsdcSolana,
    ];

    /// <summary>URI scheme → secret-key destination family for verify (match-if-any).</summary>
    internal static readonly IReadOnlyDictionary<string, IReadOnlyList<DestinationType>> SecretKeyUriFamilies =
        new Dictionary<string, IReadOnlyList<DestinationType>>
        {
            ["pol"] = PolygonEvm,
            ["eth"] = EthereumEvm,
            ["bsc"] = BscEvm,
            ["tron"] = Tron,
            ["ton"] = Ton,
            ["sol"] = Solana,
        };

    /// <summary>Address rails encrypted with a random branta_secret (not a derived key).</summary>
    internal static bool IsSecretKeyAddress(DestinationType type) =>
        type == DestinationType.BitcoinAddress || SecretKeyUriFamilies.Values.Any(family => family.Contains(type));
}
