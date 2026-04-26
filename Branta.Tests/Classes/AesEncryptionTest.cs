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

    [Fact]
    public void Encrypt_DeterministicNonce_ProducesSameOutput()
    {
        var address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
        var secret = "12345";

        var first = AesEncryption.Encrypt(address, secret, deterministicNonce: true);
        var second = AesEncryption.Encrypt(address, secret, deterministicNonce: true);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Encrypt_RandomNonce_ProducesDifferentOutput()
    {
        var address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
        var secret = "12345";

        var first = AesEncryption.Encrypt(address, secret);
        var second = AesEncryption.Encrypt(address, secret);

        Assert.NotEqual(first, second);
    }
}
