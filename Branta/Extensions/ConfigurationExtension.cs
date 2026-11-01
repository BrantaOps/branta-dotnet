using Branta;

namespace Microsoft.Extensions.DependencyInjection;

public static class BrantaServiceCollectionExtensions
{
    public static IServiceCollection ConfigureBrantaServices(this IServiceCollection services, BrantaApiOptions? defaultOptions = null)
    {
        services.AddHttpClient();

        if (defaultOptions != null)
        {
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(defaultOptions));
        }
        services.AddScoped<IBrantaService, BrantaService>();

        return services;
    }
}
