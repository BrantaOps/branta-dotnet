namespace Branta.V2.Interfaces;

public interface IAesEncryption
{
    string Encrypt(string value, string secret, bool deterministicNonce = false);
    string Decrypt(string encryptedValue, string secret);
}
