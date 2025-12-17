using Branta.Classes;
using Branta.V2.Classes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Branta.V2.Extensions;

public static class ConfigurationExtension
{
    public static IServiceCollection ConfigureBrantaServices(this IServiceCollection services, BrantaClientOptions? defaultOptions = null)
    {
        services.AddHttpClient();

        if (defaultOptions != null)
        {
            services.AddSingleton(Options.Create(defaultOptions));
        }
        services.AddScoped<BrantaClient>();

        return services;
    }
}
