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
        var baseUrl = overrideOptions?.BaseUrl ?? defaultOptions?.BaseUrl ?? throw new Exception("Branta: BaseUrl is a required option.");

        return new Uri(baseUrl.GetUrl());
    }

    public static bool IsBolt11(this string value) =>
        value.StartsWith("lnbc", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("lntb", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("lnbcrt", StringComparison.OrdinalIgnoreCase);

    public static string ToNormalizedHash(this string value)
    {
        var normalized = value.ToLowerInvariant();

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
