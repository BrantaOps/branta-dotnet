using System.Text.Json;

namespace Branta;

public class PaymentBuilder
{
    private readonly Payment payment = new()
    {
        Destinations = []
    };

    public PaymentBuilder AddDestination(string value, DestinationType? type = null)
    {
        payment.Destinations.Add(new Destination
        {
            Value = value,
            Type = type,
            Id = Guid.NewGuid().ToString()
        });

        return this;
    }

    public PaymentBuilder SetDescription(string description)
    {
        payment.Description = description;

        return this;
    }

    public PaymentBuilder AddMetadata(string key, string value)
    {
        var metadataMap = !string.IsNullOrEmpty(payment.Metadata)
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(payment.Metadata) ?? []
            : [];

        metadataMap[key] = value;
        payment.Metadata = JsonSerializer.Serialize(metadataMap);

        return this;
    }

    public PaymentBuilder SetTtl(int ttl)
    {
        payment.Ttl = ttl;

        return this;
    }

    public PaymentBuilder SetPlatformLogoUrl(string platformLogoUrl)
    {
        payment.PlatformLogoUrl = platformLogoUrl;

        return this;
    }

    public PaymentBuilder SetChildPlatform(string name, string? logoUrl = null, string? logoLightUrl = null)
    {
        payment.ChildPlatform = new Platform { Name = name, LogoUrl = logoUrl, LogoLightUrl = logoLightUrl };

        return this;
    }

    public PaymentBuilder SetBtcPayServerPluginVersion(string version)
    {
        payment.BtcPayServerPluginVersion = version;

        return this;
    }

    public Payment Build()
    {
        return payment;
    }
}
