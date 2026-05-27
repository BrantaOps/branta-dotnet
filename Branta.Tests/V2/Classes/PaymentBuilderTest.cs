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
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var destination = new Destination { Value = "addr", Type = type };
        var json = JsonSerializer.Serialize(destination, options);
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

    [Fact]
    public void SetZk_MarksLastDestinationAsZkAndAssignsZkId()
    {
        var payment = new PaymentBuilder()
            .AddDestination("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", type: DestinationType.BitcoinAddress)
            .SetZk()
            .Build();

        Assert.True(payment.Destinations[0].IsZk);
        Assert.NotNull(payment.Destinations[0].ZkId);
        Assert.NotEmpty(payment.Destinations[0].ZkId!);
    }

    [Fact]
    public void SetZk_OnlyAppliesToLastDestination()
    {
        var payment = new PaymentBuilder()
            .AddDestination("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", type: DestinationType.BitcoinAddress)
            .AddDestination("lnbc100n1ptest", type: DestinationType.Bolt11)
            .SetZk()
            .Build();

        Assert.False(payment.Destinations[0].IsZk);
        Assert.True(payment.Destinations[1].IsZk);
    }

    [Fact]
    public void SetDescription_SetsDescription()
    {
        var payment = new PaymentBuilder()
            .AddDestination("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa")
            .SetDescription("test desc")
            .Build();

        Assert.Equal("test desc", payment.Description);
    }

    [Fact]
    public void SetTtl_SetsTtl()
    {
        var payment = new PaymentBuilder()
            .AddDestination("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa")
            .SetTtl(3600)
            .Build();

        Assert.Equal(3600, payment.TTL);
    }

    [Fact]
    public void AddMetadata_AddsKeyValuePairToMetadataJson()
    {
        var payment = new PaymentBuilder()
            .AddDestination("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa")
            .AddMetadata("orderId", "123")
            .Build();

        Assert.Contains("\"orderId\"", payment.Metadata);
        Assert.Contains("\"123\"", payment.Metadata);
    }
}
