using Branta;
using Microsoft.Extensions.DependencyInjection;

namespace Branta.Tests;

public class BrantaServiceCollectionExtensionsTests
{
    [Fact]
    public void ConfigureBrantaServices_RegistersIBrantaService()
    {
        var services = new ServiceCollection()
            .ConfigureBrantaServices(new BrantaApiOptions { Environment = BrantaEnvironment.Localhost })
            .BuildServiceProvider();

        Assert.NotNull(services.GetRequiredService<IBrantaService>());
    }

    [Fact]
    public void BrantaService_CanBeConstructedFromOptions()
    {
        var service = new BrantaService(new BrantaApiOptions { Environment = BrantaEnvironment.Localhost });
        Assert.NotNull(service);
    }
}
