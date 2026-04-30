using Branta.V2.Interfaces;

namespace Branta.Classes;

public class AesEncryptionService : IAesEncryption
{
    public string Encrypt(string value, string secret, bool deterministicNonce = false)
        => AesEncryption.Encrypt(value, secret, deterministicNonce);

    public string Decrypt(string encryptedValue, string secret)
        => AesEncryption.Decrypt(encryptedValue, secret);
}
