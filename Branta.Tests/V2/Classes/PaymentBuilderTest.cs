using Branta.Enums;
using Branta.V2.Classes;
using Branta.V2.Models;
using System.Text.Json;

namespace Branta.Tests.V2.Classes;

public class PaymentBuilderTests
{
    [Fact]
    public void AddDestination_WithType_SetsTypeOnDestination()
    {
        var payment = new PaymentBuilder()
            .AddDestination("addr1", type: DestinationType.BitcoinAddress)
            .Build();

        Assert.Equal(DestinationType.BitcoinAddress, payment.Destinations[0].Type);
    }

    [Fact]
    public void AddDestination_WithoutType_TypeIsNull()
    {
        var payment = new PaymentBuilder()
            .AddDestination("addr1")
            .Build();

        Assert.Null(payment.Destinations[0].Type);
    }

    [Theory]
    [InlineData(DestinationType.BitcoinAddress, "bitcoin_address")]
    [InlineData(DestinationType.Bolt11, "bolt11")]
    [InlineData(DestinationType.Bolt12, "bolt12")]
    [InlineData(DestinationType.LnUrl, "ln_url")]
    [InlineData(DestinationType.TetherAddress, "tether_address")]
    [InlineData(DestinationType.LnAddress, "ln_address")]
    [InlineData(DestinationType.ArkAddress, "ark_address")]
    public void DestinationType_SerializesToCorrectJsonString(DestinationType type, string expected)
    {
        var destination = new Destination { Value = "addr", Type = type };
        var json = JsonSerializer.Serialize(destination);
        var doc = JsonDocument.Parse(json);

        Assert.Equal(expected, doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public void DestinationType_NullOmittedFromJson()
    {
        var destination = new Destination { Value = "addr" };
        var json = JsonSerializer.Serialize(destination);
        var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.TryGetProperty("type", out var typeProp) && typeProp.ValueKind != JsonValueKind.Null);
    }
}
