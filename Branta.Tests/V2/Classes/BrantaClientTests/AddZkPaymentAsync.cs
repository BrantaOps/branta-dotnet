using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.Extensions;
using Branta.V2.Models;
using System.Text.Json;

namespace Branta.Tests.V2.Classes.BrantaClientTests;

public class AddZkPaymentAsync : DataBrantaClientTests
{
    [Fact]
    public async Task SetsVerifyUrlOnReturnedPayment()
    {
        var payment = new Payment { Destinations = [new Destination { Value = BITCOIN_ADDRESS, IsZk = true, Type = DestinationType.BitcoinAddress }] };
        var (_, capturedRequests) = SetupCapturingHttpClientForSinglePayment(SingleBitcoinAddress);

        var (result, _) = await _brantaClient.AddZKPaymentAsync(payment);

        var sentBody = await capturedRequests[0].Content!.ReadAsStringAsync();
        var sentPayment = JsonSerializer.Deserialize<Payment>(sentBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var encryptedAddress = sentPayment!.Destinations[0].Value;
        var baseUrl = BrantaServerBaseUrl.Localhost.GetUrl().TrimEnd('/');
        Assert.Equal($"{baseUrl}/v2/verify/{Uri.EscapeDataString(encryptedAddress)}", result?.VerifyUrl);
    }

    [Fact]
    public async Task UnsupportedType_ThrowsBrantaPaymentException()
    {
        var payment = new Payment { Destinations = [new Destination { Value = "lnurl1234", IsZk = true, Type = DestinationType.LnUrl }] };

        await Assert.ThrowsAsync<BrantaPaymentException>(() => _brantaClient.AddZKPaymentAsync(payment));
    }

    [Fact]
    public async Task Bolt11_ValueIsEncryptedInvoice()
    {
        var payment = new Payment { Destinations = [new Destination { Value = BOLT_11, IsZk = true, Type = DestinationType.Bolt11 }] };
        var (_, capturedRequests) = SetupCapturingHttpClientForSinglePayment(SingleBolt11Invoice);

        await _brantaClient.AddZKPaymentAsync(payment);

        var sentBody = await capturedRequests[0].Content!.ReadAsStringAsync();
        var sentPayment = JsonSerializer.Deserialize<Payment>(sentBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var hash = BOLT_11.ToNormalizedHash();
        Assert.Equal(BOLT_11, AesEncryption.Decrypt(sentPayment!.Destinations[0].Value, hash));
    }
}
