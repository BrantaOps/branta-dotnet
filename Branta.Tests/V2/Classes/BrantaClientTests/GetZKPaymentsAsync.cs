using Branta.Extensions;

namespace Branta.Tests.V2.Classes.BrantaClientTests;

public class GetZKPaymentsAsync : DataBrantaClientTests
{
    [Fact]
    public async Task GetZKPaymentAsync_WithZkDestinations_DecryptsValues()
    {
        var payment = EncryptedBitcoinAddress;
        var address = "some-address";
        SetupHttpClient([payment, GetSingleBitcoinAddress(address)]);

        var result = await _brantaClient.GetZKPaymentsAsync(payment.GetDefaultValue(), ENCRYPTED_BITCOIN_SECRET);

        Assert.Equal(BITCOIN_ADDRESS, result.First().GetDefaultValue());
        Assert.Equal(address, result.Last().GetDefaultValue());
    }

    [Fact]
    public async Task GetZKPaymentAsync_WithZkDestinations_ReturnsValidUrl()
    {
        var payment = EncryptedBitcoinAddress;
        var address = "some-address";
        SetupHttpClient([payment, GetSingleBitcoinAddress(address)]);

        var result = await _brantaClient.GetZKPaymentsAsync(payment.GetDefaultValue(), ENCRYPTED_BITCOIN_SECRET);

        var expectedUrl = $"http://localhost:3000/v2/verify/{Uri.EscapeDataString(ENCRYPTED_BITCOIN_ADDRESS)}#k-{payment.Destinations.First().ZkId}={ENCRYPTED_BITCOIN_SECRET}";
        Assert.Equal(expectedUrl, result.First().VerifyUrl);
    }


    [Fact]
    public async Task GetZKPaymentAsync_WithBolt11Zk_DecryptsValues()
    {
        var payment = EncryptedBolt11Invoice;
        SetupHttpClient(payment);

        var result = await _brantaClient.GetZKPaymentsWithHashSecretAsync(BOLT_11);

        Assert.Equal(BOLT_11, result.First().GetDefaultValue());
    }

    [Fact]
    public async Task GetZKPaymentAsync_WithBolt11Zk_ReturnsValidUrl()
    {
        var payment = EncryptedBolt11Invoice;
        SetupHttpClient(payment);

        var result = await _brantaClient.GetZKPaymentsWithHashSecretAsync(BOLT_11);

        var destination = EncryptedBolt11Invoice.Destinations.First();
        var fragment = $"k-{destination.ZkId}={BOLT_11.ToNormalizedHash()}";
        Assert.Equal($"http://localhost:3000/v2/verify/{Uri.EscapeDataString(EncryptedBolt11Invoice.GetDefaultValue())}#{fragment}", result.First().VerifyUrl);
    }
}
