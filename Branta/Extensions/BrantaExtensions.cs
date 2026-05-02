using Branta.Attributes;
using Branta.Classes;
using Branta.Enums;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Branta.Extensions;

public static class BrantaExtensions
{
    public static string GetUrl(this BrantaServerBaseUrl server)
    {
        var field = server.GetType().GetField(server.ToString());
        var attribute = field?.GetCustomAttribute<UrlAttribute>();
        return attribute?.Url ?? throw new ArgumentException($"No URL defined for {server}");
    }

    public static Uri GetUri(this BrantaClientOptions? defaultOptions, BrantaClientOptions? overrideOptions)
    {
        return new Uri(defaultOptions.GetBaseUrl(overrideOptions));
    }

    public static string GetBaseUrl(this BrantaClientOptions? defaultOptions, BrantaClientOptions? overrideOptions)
    {
        var baseUrl = overrideOptions?.BaseUrl ?? defaultOptions?.BaseUrl ?? throw new Exception("Branta: BaseUrl is a required option.");

        return baseUrl.GetUrl();
    }

    public static PrivacyMode GetPrivacy(this BrantaClientOptions? defaultOptions, BrantaClientOptions? overrideOptions, PrivacyMode fallback = PrivacyMode.Strict)
        => overrideOptions?.Privacy ?? defaultOptions?.Privacy ?? fallback;

    public static string? GetApiKey(this BrantaClientOptions? defaultOptions, BrantaClientOptions? overrideOptions)
        => overrideOptions?.DefaultApiKey ?? defaultOptions?.DefaultApiKey;

    public static string? GetHmacSecret(this BrantaClientOptions? defaultOptions, BrantaClientOptions? overrideOptions)
        => overrideOptions?.HmacSecret ?? defaultOptions?.HmacSecret;

    public static bool IsBolt11(this string value) =>
        value.StartsWith("lnbc", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("lntb", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("lnbcrt", StringComparison.OrdinalIgnoreCase);

    public static bool IsArk(this string value) => value.StartsWith("ark1", StringComparison.OrdinalIgnoreCase);

    public static DestinationType? GetHashZkType(this string value)
    {
        if (value.IsBolt11()) return DestinationType.Bolt11;
        if (value.IsArk()) return DestinationType.ArkAddress;
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
