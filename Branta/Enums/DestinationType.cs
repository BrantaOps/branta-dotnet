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
    UsdcSolana,
    [JsonPropertyName("usdt_base")]
    UsdtBase,
    [JsonPropertyName("usdc_base")]
    UsdcBase,
    [JsonPropertyName("usdt_arbitrum")]
    UsdtArbitrum,
    [JsonPropertyName("usdc_arbitrum")]
    UsdcArbitrum
}

/// <summary>An ERC-681 <c>ethereum:&lt;contract&gt;@&lt;chainId&gt;/transfer</c> rail.</summary>
internal sealed record EvmTokenRail(long ChainId, string Contract, DestinationType Type);

/// <summary>
/// Internal helpers for destination types used by verify and encrypt.
/// </summary>
internal static class DestinationTypes
{
    /// <summary>
    /// Chain id plus token contract select exactly one destination type.
    /// Contracts are lowercase so lookup is case-insensitive.
    /// </summary>
    internal static readonly IReadOnlyList<EvmTokenRail> EvmTokenRails =
    [
        new(1, "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48", DestinationType.UsdcEthereum),
        new(1, "0xdac17f958d2ee523a2206206994597c13d831ec7", DestinationType.UsdtEthereum),
        new(137, "0x3c499c542cef5e3811e1192ce70d8cc03d5c3359", DestinationType.UsdcPolygon),
        new(137, "0xc2132d05d31c914a87c6611c10748aeb04b58e8f", DestinationType.UsdtPolygon),
        new(8453, "0x833589fcd6edb6e08f4c7c32d4f71b54bda02913", DestinationType.UsdcBase),
        new(8453, "0xfde4c96c8593536e31f229ea8f37b2ada2699bb2", DestinationType.UsdtBase),
        new(42161, "0xaf88d065e77c8cc2239327c5edb3a432268e5831", DestinationType.UsdcArbitrum),
        new(42161, "0xfd086bc7cd5c481dcc9c85ebe478a1c0b69fcbb9", DestinationType.UsdtArbitrum),
        new(56, "0x8ac76a51cc950d9822d68b83fe1ad97b32cd580d", DestinationType.UsdcBsc),
        new(56, "0x55d398326f99059ff775485246999027b3197955", DestinationType.UsdtBsc),
    ];

    /// <summary>The destination type for a chain id and token contract, if they name a rail.</summary>
    internal static DestinationType? EvmTokenType(long chainId, string contract)
    {
        var needle = contract.ToLowerInvariant();
        foreach (var rail in EvmTokenRails)
        {
            if (rail.ChainId == chainId && rail.Contract == needle) return rail.Type;
        }
        return null;
    }

    // Tron, TON, and Solana still encrypt on AddPaymentAsync. They have no verify URI.
    private static readonly HashSet<DestinationType> UuidSecretTypes =
    [
        DestinationType.BitcoinAddress,
        DestinationType.UsdtTron,
        DestinationType.UsdtTon,
        DestinationType.UsdtSolana,
        DestinationType.UsdcSolana,
    ];

    /// <summary>Address rails encrypted with one random UUID secret (not a derived key).</summary>
    internal static bool IsSecretKeyAddress(DestinationType type) =>
        UuidSecretTypes.Contains(type) || EvmTokenRails.Any(rail => rail.Type == type);
}
