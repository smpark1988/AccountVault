using System.Security.Cryptography;
using System.Text;
using AccountVault.Models;

namespace AccountVault.Services;

public sealed class CryptoService
{
    public const int DefaultIterations = 300_000;
    public const int SaltSize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int KeySize = 32;

    public byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltSize);

    public byte[] GenerateNonce() => RandomNumberGenerator.GetBytes(NonceSize);

    // PBKDF2-SHA256 derives the AES-256 key from the master password and salt.
    public byte[] DeriveKey(string masterPassword, byte[] salt, int iterations)
    {
        if (iterations < DefaultIterations)
        {
            throw new VaultException("PBKDF2 반복 횟수가 보안 기준보다 낮습니다.");
        }

        using var pbkdf2 = new Rfc2898DeriveBytes(
            masterPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(KeySize);
    }

    public EncryptedPayload Encrypt(string json, byte[] key)
    {
        ValidateKey(key);

        var nonce = GenerateNonce();
        var tag = new byte[TagSize];
        var plaintext = Encoding.UTF8.GetBytes(json);
        var cipherText = new byte[plaintext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, cipherText, tag);

            return new EncryptedPayload
            {
                Nonce = nonce,
                Tag = tag,
                CipherText = cipherText
            };
        }
        finally
        {
            Array.Clear(plaintext);
        }
    }

    public string Decrypt(VaultFile vaultFile, byte[] key)
    {
        ValidateKey(key);

        var nonce = Convert.FromBase64String(vaultFile.Nonce);
        var tag = Convert.FromBase64String(vaultFile.Tag);
        var cipherText = Convert.FromBase64String(vaultFile.CipherText);

        if (nonce.Length != NonceSize || tag.Length != TagSize)
        {
            throw new CryptographicException("암호화 메타데이터 길이가 올바르지 않습니다.");
        }

        var plaintext = new byte[cipherText.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipherText, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            Array.Clear(plaintext);
        }
    }

    private static void ValidateKey(byte[] key)
    {
        if (key.Length != KeySize)
        {
            throw new VaultException("암호화 키 길이가 올바르지 않습니다.");
        }
    }
}
