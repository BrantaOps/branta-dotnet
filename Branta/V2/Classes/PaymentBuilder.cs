using Branta.Enums;
using Branta.V2.Models;
using System.Text.Json;

namespace Branta.V2.Classes;

public class PaymentBuilder
{
    private readonly Payment payment = new()
    {
        Destinations = []
    };

    public PaymentBuilder AddDestination(string address, DestinationType? type = null)
    {
        payment.Destinations.Add(new Destination()
        {
            Value = address,
            Type = type,
            IsZk = false
        });

        return this;
    }

    public PaymentBuilder SetZk()
    {
        var destination = payment.Destinations?.Last();

        if (destination != null)
        {
            destination.IsZk = true;
            destination.ZkId = Guid.NewGuid().ToString();
        }

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
        payment.TTL = ttl;

        return this;
    }

    public PaymentBuilder SetPlatformLogoUrl(string platformLogoUrl)
    {
        payment.PlatformLogoUrl = platformLogoUrl;

        return this;
    }

    public Payment Build()
    {
        return payment;
    }
}
