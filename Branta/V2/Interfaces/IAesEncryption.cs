namespace Branta.V2.Interfaces;

internal interface IAesEncryption
{
    string Encrypt(string value, string secret, bool deterministicNonce = false);
    string Decrypt(string encryptedValue, string secret);
}
