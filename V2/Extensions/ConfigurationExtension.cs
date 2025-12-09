using Branta.V2.Classes;
using Microsoft.Extensions.DependencyInjection;

namespace Branta.V2.Extensions;

public static class ConfigurationExtension
{
    public static IServiceCollection ConfigureBrantaServices(this IServiceCollection services, string baseUrl = "https://guardrail.branta.pro")
    {
        services.AddHttpClient<BrantaClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}
