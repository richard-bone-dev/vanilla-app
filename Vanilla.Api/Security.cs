using System.Security.Cryptography;
using System.Text;

namespace Vanilla.Api.Security;

public interface IFieldEncryptionService
{
    string? Encrypt(string? plaintext);
    string? Decrypt(string? ciphertext);
}

public sealed class AesGcmFieldEncryptionService : IFieldEncryptionService
{
    private const string Prefix = "enc:v1:";
    private readonly byte[] _key;

    public AesGcmFieldEncryptionService(string configuredKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredKey);
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
    }

    public string? Encrypt(string? plaintext)
    {
        if (plaintext is null)
        {
            return null;
        }

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aesGcm = new AesGcm(_key, 16);
        aesGcm.Encrypt(nonce, plaintextBytes, cipherBytes, tag);

        var payload = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length + tag.Length, cipherBytes.Length);

        return Prefix + Convert.ToBase64String(payload);
    }

    public string? Decrypt(string? ciphertext)
    {
        if (ciphertext is null)
        {
            return null;
        }

        if (!ciphertext.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return ciphertext;
        }

        var payload = Convert.FromBase64String(ciphertext[Prefix.Length..]);
        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipherBytes = payload[28..];
        var plaintextBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, 16);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
