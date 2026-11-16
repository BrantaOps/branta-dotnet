using Branta;

namespace Branta.Tests.Classes;

public class AesEncryptionTest
{
    // Fixed vectors. Every Branta SDK asserts the same strings, so a payment written by one SDK
    // stays readable and findable by every other. Do not regenerate these from this code.
    [Fact]
    public void Encrypt_FixedVector_MatchesEverySdk()
    {
        var aes = new AesEncryption();

        Assert.Equal(
            "mPIKHc3ywVlsBHf3Lv2Rwpz2+fKE0kgUePq2m4fPIUidMuGEHVIB",
            aes.Encrypt("hello world", "my-secret", deterministicNonce: true));
    }

    [Fact]
    public void Decrypt_FixedVector_ReturnsPlaintext()
    {
        var aes = new AesEncryption();

        Assert.Equal(
            "hello world",
            aes.Decrypt("mPIKHc3ywVlsBHf3Lv2Rwpz2+fKE0kgUePq2m4fPIUidMuGEHVIB", "my-secret"));
    }

    [Fact]
    public void Encrypt_DerivedKeyFixedVector_MatchesEverySdk()
    {
        // Key is the normalized hash of "LNBC10U1PTestInvoice". The value is encrypted lowercased.
        var key = "261D7974DC0541402A222AC17973D3FF412878C1C179FCD4D866094D610B4496";
        var aes = new AesEncryption();

        Assert.Equal(
            "WywBRexAiHTi2mnHK/W1DIN79bF1XVhU3BdC3DWpUsdhs5NMbdyZyzWsdjfuFgIV",
            aes.Encrypt("lnbc10u1ptestinvoice", key, deterministicNonce: true));
    }

    [Fact]
    public void EncryptAndDecrypt()
    {
        var address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
        var secret = "12345";

        var aes = new AesEncryption();
        var encryptedValue = aes.Encrypt(address, secret);

        Assert.Equal(address, aes.Decrypt(encryptedValue, secret));
    }

    [Fact]
    public void Encrypt_DeterministicNonce_ProducesSameOutput()
    {
        var address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
        var secret = "12345";

        var aes = new AesEncryption();
        var first = aes.Encrypt(address, secret, deterministicNonce: true);
        var second = aes.Encrypt(address, secret, deterministicNonce: true);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Encrypt_RandomNonce_ProducesDifferentOutput()
    {
        var address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
        var secret = "12345";

        var aes = new AesEncryption();
        var first = aes.Encrypt(address, secret);
        var second = aes.Encrypt(address, secret);

        Assert.NotEqual(first, second);
    }
}
