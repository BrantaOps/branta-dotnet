using System.Security.Cryptography;
using System.Text;

namespace Branta.Classes;

public class AesEncryption
{
    public static string Encrypt(string value, string secret, bool deterministicNonce = false)
    {
        try
        {
            byte[] keyData = SHA256.HashData(Encoding.UTF8.GetBytes(secret));

            byte[] iv = new byte[12];
            if (!deterministicNonce)
            {
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(iv);
            }
            else
            {
                byte[] derived = HMACSHA256.HashData(keyData, Encoding.UTF8.GetBytes(value));
                Buffer.BlockCopy(derived, 0, iv, 0, iv.Length);
            }

            byte[] plaintext = Encoding.UTF8.GetBytes(value);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            using (AesGcm aesGcm = new(keyData, 16))
            {
                aesGcm.Encrypt(iv, plaintext, ciphertext, tag);
            }

            byte[] result = new byte[iv.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, iv.Length + ciphertext.Length, tag.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception e)
        {
            throw new Exception("Encryption failed: " + e.Message, e);
        }
    }

    public static string Decrypt(string encryptedValue, string secret)
    {
        try
        {
            byte[] encryptedData = Convert.FromBase64String(encryptedValue);

            if (encryptedData.Length < 28)
                throw new ArgumentException("Invalid encrypted data: too short");

            byte[] keyData = SHA256.HashData(Encoding.UTF8.GetBytes(secret));

            byte[] iv = new byte[12];
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[encryptedData.Length - iv.Length - tag.Length];

            Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(encryptedData, iv.Length, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(encryptedData, iv.Length + ciphertext.Length, tag, 0, tag.Length);

            byte[] plaintext = new byte[ciphertext.Length];
            using (AesGcm aesGcm = new(keyData, 16))
            {
                aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
            }

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception e) when (e is not ArgumentException)
        {
            throw new Exception("Decryption failed: " + e.Message, e);
        }
    }
}
