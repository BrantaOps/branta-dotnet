using Branta.Classes;
using Branta.Enums;
using Branta.Extensions;
using Branta.V2.Classes;
using Branta.V2.Models;

namespace Branta.Tests.V2.Classes.BrantaClientTests;

public class DataBrantaClientTests : BaseBrantaClientTests
{
    public const string BITCOIN_ADDRESS = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
    public const string ENCRYPTED_BITCOIN_ADDRESS = "idaZfzsAvZDBGWSy1Xq6VKiCLwKz0HD/5S6IflXw8nEtg7NjxbgtAzQHTtHH0jyU6nfqppiXTon/A1nfCEg=";
    public const string ENCRYPTED_BITCOIN_SECRET = "1234";

    public Payment SingleBitcoinAddress = GetSingleBitcoinAddress(BITCOIN_ADDRESS);

    public Payment EncryptedBitcoinAddress = new PaymentBuilder()
        .AddDestination(ENCRYPTED_BITCOIN_ADDRESS, type: DestinationType.BitcoinAddress)
        .SetZk()
        .Build();

    public const string BOLT_11 = "lnbc15u1p3xnhl2pp5jptserfk3zk4qy42tlucycrfwxhydvlemu9pqr93tuzlv9cc7g3sdqsvfhkcap3xyhx7un8cqzpgxqzjcsp5f8c52y2stc300gl6s4xswtjpc37hrnnr3c9wvtgjfuvqmpm35evq9qyyssqy4lgd8tj637qcjp05rdpxxykjenthxftej7a2zzmwrmrl70fyj9hvj0rewhzj7jfyuwkwcg9g2jpwtk3wkjtwnkdks84hsnu8xps5vsq4gj5hs";

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
