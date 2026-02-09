using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace PaymentGateway.Application.Encryption;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IOptions<EncryptionOptions> options)
    {
        var keyString = options.Value.Key;

        if (string.IsNullOrEmpty(keyString))
        {
            throw new InvalidOperationException("Encryption key is not configured. Set the 'Encryption:Key' configuration value.");
        }

        _key = DeriveKey(keyString);
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // IV is prepended to ciphertext so it can be extracted during decryption
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return ciphertext;
        }

        var cipherBytes = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[aes.BlockSize / 8];
        var encryptedContent = new byte[cipherBytes.Length - iv.Length];

        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, iv.Length, encryptedContent, 0, encryptedContent.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(encryptedContent, 0, encryptedContent.Length);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static byte[] DeriveKey(string passphrase)
    {
        var salt = "PaymentGateway.Salt.v1"u8.ToArray();
        using var pbkdf2 = new Rfc2898DeriveBytes(passphrase, salt, iterations: 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }
}