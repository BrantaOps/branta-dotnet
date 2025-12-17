using Branta.Attributes;
using Branta.Enums;
using System.Reflection;

namespace Branta.Extensions;

public static class BrantaExtensions
{
    public static string GetUrl(this BrantaServerBaseUrl server)
    {
        var field = server.GetType().GetField(server.ToString());
        var attribute = field?.GetCustomAttribute<UrlAttribute>();
        return attribute?.Url ?? throw new ArgumentException($"No URL defined for {server}");
    }
}
