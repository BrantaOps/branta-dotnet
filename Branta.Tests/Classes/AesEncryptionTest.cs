using Branta.Classes;

namespace Branta.Tests.Classes;

public class AesEncryptionTest
{
    [Fact]
    public void EncryptAndDecrypt()
    {
        var address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
        var secret = "12345";

        var encryptedValue = AesEncryption.Encrypt(address, secret);

        Assert.Equal(address, AesEncryption.Decrypt(encryptedValue, secret));
    }
}
