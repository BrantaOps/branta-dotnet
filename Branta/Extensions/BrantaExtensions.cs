using System.Security.Cryptography;
using System.Text;

namespace Branta;

internal static class BrantaExtensions
{
    public static string GetUrl(this BrantaEnvironment environment) => environment switch
    {
        BrantaEnvironment.Staging => "https://staging.guardrail.branta.pro",
        BrantaEnvironment.Production => "https://guardrail.branta.pro",
        BrantaEnvironment.Localhost => "http://localhost:3000",
        _ => throw new ArgumentException($"No URL defined for {environment}")
    };

    public static Uri GetUri(this BrantaApiOptions? defaultOptions, BrantaApiOptions? overrideOptions)
    {
        return new Uri(defaultOptions.GetBaseUrl(overrideOptions));
    }

    public static string GetBaseUrl(this BrantaApiOptions? defaultOptions, BrantaApiOptions? overrideOptions)
    {
        var environment = overrideOptions?.Environment ?? defaultOptions?.Environment
            ?? throw new Exception("Branta: Environment is a required option.");

        return environment.GetUrl();
    }

    public static string? GetApiKey(this BrantaApiOptions? defaultOptions, BrantaApiOptions? overrideOptions)
        => overrideOptions?.ApiKey ?? defaultOptions?.ApiKey;

#pragma warning disable CS0618 // HmacSecret is obsolete; still resolved for legacy parent platforms.
    public static string? GetHmacSecret(this BrantaApiOptions? defaultOptions, BrantaApiOptions? overrideOptions)
        => overrideOptions?.HmacSecret ?? defaultOptions?.HmacSecret;
#pragma warning restore CS0618

    public static bool IsBolt11(this string value) =>
        value.StartsWith("lnbc", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("lntb", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("lnbcrt", StringComparison.OrdinalIgnoreCase);

    public static bool IsArk(this string value) =>
        value.StartsWith("ark1", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("tark1", StringComparison.OrdinalIgnoreCase);

    public static bool IsSilentPayment(this string value) =>
        value.StartsWith("sp1", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("tsp1", StringComparison.OrdinalIgnoreCase);

    public static DestinationType? GetDerivedKeyType(this string value)
    {
        if (value.IsBolt11()) return DestinationType.Bolt11;
        if (value.IsArk()) return DestinationType.ArkAddress;
        if (value.IsSilentPayment()) return DestinationType.SilentPayment;
        return null;
    }

    public static string ToNormalizedHash(this string value)
    {
        var normalized = value.ToLowerInvariant();

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    public static string ToUrlFragment(this Dictionary<string, string> keys)
    {
        var fragments = keys.Select(key => $"k-{key.Key}={key.Value}");

        return "#" + string.Join("&", fragments);
    }
}
