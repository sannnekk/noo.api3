using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Security;

[RegisterSingleton(typeof(ISecretProtector))]
public class AesGcmSecretProtector : ISecretProtector
{
    private const int _nonceSize = 12;
    private const int _tagSize = 16;
    private const int _keySize = 32;

    private readonly byte[] _key;

    public AesGcmSecretProtector(IOptions<GoogleConfig> googleConfig)
    {
        _key = DeriveKey(googleConfig.Value.TokenEncryptionKey);
    }

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(_nonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[_tagSize];

        using var aes = new AesGcm(_key, _tagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[_nonceSize + _tagSize + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, _nonceSize);
        ciphertext.CopyTo(payload, _nonceSize + _tagSize);

        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);

        byte[] payload;

        try
        {
            payload = Convert.FromBase64String(protectedValue);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("Protected value is not valid base64.", exception);
        }

        if (payload.Length < _nonceSize + _tagSize)
        {
            throw new CryptographicException("Protected value is too short to be valid.");
        }

        var nonce = payload.AsSpan(0, _nonceSize);
        var tag = payload.AsSpan(_nonceSize, _tagSize);
        var ciphertext = payload.AsSpan(_nonceSize + _tagSize);
        var plaintextBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, _tagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static byte[] DeriveKey(string configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException(
                $"{nameof(GoogleConfig)}.{nameof(GoogleConfig.TokenEncryptionKey)} must be set."
            );
        }

        if (
            Convert.TryFromBase64String(configuredKey, new byte[_keySize], out var written)
            && written == _keySize
        )
        {
            return Convert.FromBase64String(configuredKey);
        }

        throw new InvalidOperationException(
            $"{nameof(GoogleConfig)}.{nameof(GoogleConfig.TokenEncryptionKey)} must be a base64-encoded 256-bit key. "
                + "Generate one with: openssl rand -base64 32"
        );
    }
}
