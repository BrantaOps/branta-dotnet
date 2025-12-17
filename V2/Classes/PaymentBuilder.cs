using Branta.V2.Models;
using System.Text.Json;

namespace Branta.V2.Classes;

public class PaymentBuilder
{
    private readonly Payment payment = new()
    {
        Destinations = []
    };

    public PaymentBuilder AddDestination(string address, bool zk = false)
    {
        payment.Destinations.Add(new Destination()
        {
            Value = address,
            IsZk = zk
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
        payment.TTL = ttl;

        return this;
    }

    public Payment Build()
    {
        return payment;
    }
}
