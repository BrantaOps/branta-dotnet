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

    [Fact]
    public void AddDestination_AssignsId()
    {
        var payment = new PaymentBuilder()
            .AddDestination("addr1", type: DestinationType.BitcoinAddress)
            .Build();

        Assert.False(string.IsNullOrEmpty(payment.Destinations[0].Id));
    }

    [Theory]
    [InlineData(DestinationType.BitcoinAddress, "bitcoin_address")]
    [InlineData(DestinationType.Bolt11, "bolt11")]
    [InlineData(DestinationType.ArkAddress, "ark_address")]
    [InlineData(DestinationType.SilentPayment, "silent_payment")]
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
    public void Destination_SerializesZkTrueAndZkId()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var destination = new Destination { Value = "addr", Id = "id-1" };
        var json = JsonSerializer.Serialize(destination, options);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("zk").GetBoolean());
        Assert.Equal("id-1", doc.RootElement.GetProperty("zk_id").GetString());
    }

    [Fact]
    public void Destination_DeserializesZkFalseAsNotEncrypted()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var destination = JsonSerializer.Deserialize<Destination>("""{"value":"addr","zk":false}""", options);

        Assert.False(destination!.IsEncrypted);
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

    [Fact]
    public void SetChildPlatform_SetsName()
    {
        var payment = new PaymentBuilder()
            .AddDestination("bc1qtest")
            .SetChildPlatform("Acme")
            .Build();

        Assert.Equal("Acme", payment.ChildPlatform!.Name);
    }

    [Fact]
    public void SetChildPlatform_OptionalUrlsDefaultNull()
    {
        var payment = new PaymentBuilder()
            .AddDestination("bc1qtest")
            .SetChildPlatform("Acme")
            .Build();

        Assert.Null(payment.ChildPlatform!.LogoUrl);
        Assert.Null(payment.ChildPlatform.LogoLightUrl);
    }

    [Fact]
    public void SetChildPlatform_WithUrls_SetsUrls()
    {
        var payment = new PaymentBuilder()
            .AddDestination("bc1qtest")
            .SetChildPlatform("Acme", logoUrl: "https://example.com/logo.png", logoLightUrl: "https://example.com/logo-light.png")
            .Build();

        Assert.Equal("https://example.com/logo.png", payment.ChildPlatform!.LogoUrl);
        Assert.Equal("https://example.com/logo-light.png", payment.ChildPlatform.LogoLightUrl);
    }

    [Fact]
    public void SetChildPlatform_ReturnsBuilder()
    {
        var builder = new PaymentBuilder().AddDestination("bc1qtest");
        var result = builder.SetChildPlatform("Acme");

        Assert.Same(builder, result);
    }
}
