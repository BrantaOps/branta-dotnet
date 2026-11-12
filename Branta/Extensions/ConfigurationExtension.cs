using Branta;

namespace Microsoft.Extensions.DependencyInjection;

public static class BrantaServiceCollectionExtensions
{
    public static IServiceCollection ConfigureBrantaServices(this IServiceCollection services, BrantaApiOptions defaultOptions)
    {
        services.ConfigureHttpClientDefaults(builder =>
            builder.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30)));
        services.AddHttpClient();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(defaultOptions));
        services.AddScoped<IBrantaService>(sp => new BrantaService(
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BrantaApiOptions>>().Value,
            sp.GetRequiredService<IHttpClientFactory>()));

        return services;
    }
}
