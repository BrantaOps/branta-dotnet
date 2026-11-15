using System.Reflection;
using Branta;

namespace Branta.Tests;

/// <summary>
/// Consumer-surface lock. Family helpers and HTTP/crypto internals stay internal.
/// </summary>
public class PublicApiTests
{
    private static readonly HashSet<string> AllowedExported =
    [
        "Branta.AddPaymentResult",
        "Branta.BrantaApiOptions",
        "Branta.BrantaEnvironment",
        "Branta.BrantaPaymentException",
        "Branta.BrantaService",
        "Branta.Destination",
        "Branta.DestinationType",
        "Branta.IBrantaService",
        "Branta.Payment",
        "Branta.PaymentBuilder",
        "Branta.Platform",
        "Branta.VerifyResult",
        "Branta.VerifyStatus",
        "Microsoft.Extensions.DependencyInjection.BrantaServiceCollectionExtensions",
    ];

    [Fact]
    public void ExportedTypes_MatchConsumerSurface()
    {
        var exported = typeof(BrantaService).Assembly
            .GetExportedTypes()
            .Select(t => t.FullName!)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(AllowedExported.OrderBy(n => n).ToArray(), exported);
    }

    [Fact]
    public void Internals_AreNotExported()
    {
        var exported = typeof(BrantaService).Assembly.GetExportedTypes().Select(t => t.Name).ToHashSet();
        Assert.DoesNotContain("DestinationTypes", exported);
        Assert.DoesNotContain("EvmTokenRail", exported);
        Assert.DoesNotContain("BrantaClient", exported);
        Assert.DoesNotContain("QRParser", exported);
        Assert.DoesNotContain("AesEncryption", exported);
        Assert.DoesNotContain("IncompleteError", exported);
        Assert.DoesNotContain("TamperedError", exported);
    }

    [Fact]
    public void DestinationType_HasEighteenMembers_AndNoUsdcTon()
    {
        Assert.Equal(18, Enum.GetValues<DestinationType>().Length);
        Assert.Equal(DestinationType.UsdtTon, Enum.Parse<DestinationType>("UsdtTon"));
        Assert.Throws<ArgumentException>(() => Enum.Parse<DestinationType>("UsdcTon"));
        Assert.Null(DestinationTypeConverter.TryParse("usdc_ton"));
    }

    [Fact]
    public void ConsumerTypes_AreReachable()
    {
        var options = new BrantaApiOptions { Environment = BrantaEnvironment.Staging, ApiKey = "k" };
        Assert.Equal("k", options.ApiKey);

        var service = new BrantaService(BrantaEnvironment.Staging);
        Assert.NotNull(service);

        var payment = new PaymentBuilder()
            .AddDestination("bc1qexample", DestinationType.BitcoinAddress)
            .Build();
        Assert.Single(payment.Destinations);

        var verify = new VerifyResult { Status = VerifyStatus.Unknown, VerifyUrl = "https://example/v2/verify/x" };
        Assert.Equal(VerifyStatus.Unknown, verify.Status);
        Assert.Null(verify.MatchedDestinations);

        var error = new BrantaPaymentException("platformLogoUrl domain does not match the configured API domain");
        Assert.Contains("platformLogoUrl", error.Message);
    }

    [Fact]
    public void DestinationTypes_IsInternal()
    {
        var type = typeof(BrantaService).Assembly.GetType("Branta.DestinationTypes");
        Assert.NotNull(type);
        Assert.False(type!.IsPublic);
        Assert.True(type.IsNotPublic);
        // The rail table and its helpers stay internal.
        Assert.Null(type.GetMethod("IsPolygonEvm", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic));
        Assert.Null(type.GetField("SecretKeyUriFamilies", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic));
        Assert.Null(type.GetMethod("EvmTokenType", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(type.GetField("EvmTokenRails", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(type.GetMethod("IsSecretKeyAddress", BindingFlags.Public | BindingFlags.Static));
    }
}
