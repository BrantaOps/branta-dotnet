using Branta.Classes;
using Branta.Enums;
using Branta.Extensions;
using Branta.V2.Classes;
using Branta.V2.Models;

namespace Branta.Tests.V2.Classes.BrantaClientTests;

public class DataBrantaClientTests : BaseBrantaClientTests
{
    public const string BITCOIN_ADDRESS = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
    public const string ENCRYPTED_BITCOIN_ADDRESS = "pQerSFV+fievHP+guYoGJjx1CzFFrYWHAgWrLhn5473Z19M6+WMScLd1hsk808AEF/x+GpZKmNacFBf5BbQ=";
    public const string ENCRYPTED_BITCOIN_SECRET = "1234";

    public Payment SingleBitcoinAddress = GetSingleBitcoinAddress(BITCOIN_ADDRESS);

    public Payment EncryptedBitcoinAddress = new PaymentBuilder()
        .AddDestination(ENCRYPTED_BITCOIN_ADDRESS, type: DestinationType.BitcoinAddress)
        .SetZk()
        .Build();

    public const string BOLT_11 = "lnbc100n1pn4znpptest";

    public Payment SingleBolt11Invoice = new PaymentBuilder()
        .AddDestination(BOLT_11, type: DestinationType.Bolt11)
        .Build();

    public Payment EncryptedBolt11Invoice = new PaymentBuilder()
        .AddDestination(AesEncryption.Encrypt(BOLT_11, BOLT_11.ToNormalizedHash(), deterministicNonce: true), type: DestinationType.Bolt11)
        .SetZk()
        .Build();

    public static Payment GetSingleBitcoinAddress(string address)
    {
        return new PaymentBuilder()
            .AddDestination(address, type: DestinationType.BitcoinAddress)
            .Build();
    }

    protected static BrantaClientOptions OptionsWithHmac(string hmacSecret = "test-hmac-secret") => new()
    {
        BaseUrl = BrantaServerBaseUrl.Localhost,
        DefaultApiKey = "test-api-key",
        Privacy = PrivacyMode.Loose,
        HmacSecret = hmacSecret
    };
}
