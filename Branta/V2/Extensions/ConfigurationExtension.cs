using Branta.Classes;
using Branta.V2.Classes;
using Branta.V2.Interfaces;
using Branta.V2.Services;
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
        services.AddScoped<IAesEncryption, AesEncryptionService>();
        services.AddSingleton<ISecretGenerator, GuidSecretGenerator>();
        services.AddScoped<IBrantaClient, BrantaClient>();
        services.AddScoped<IBrantaService, BrantaService>();

        return services;
    }
}
