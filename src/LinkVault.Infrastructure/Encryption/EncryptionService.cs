using System.Security.Cryptography;
using System.Text;
using LinkVault.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace LinkVault.Infrastructure.Encryption;

public sealed class EncryptionService(IVaultKeyProvider vaultKeyProvider, IMemoryCache cache) : IEncryptionService
{
    private const int AesKeySize = 32;     
    private const int GcmNonceSize = 12;    
    private const int GcmTagSize = 16;
    private byte[]? _cachedKek;
    private byte[]? _cachedSaltIndexHash; 
    private readonly IVaultKeyProvider _vaultKeyProvider = vaultKeyProvider;
    private readonly IMemoryCache _cache = cache;

    public string GenerateEncryptedDek()
    {
        var dek = RandomNumberGenerator.GetBytes(AesKeySize);
        var kek = GetKek();
        var encrypted = AesGcmEncryptBytes(dek, kek);
        return Convert.ToBase64String(encrypted);
    }

    public string DecryptDek(string encryptedDek)
    {
        string cacheDek = $"dek_plain_{encryptedDek.GetHashCode()}";

        var result = _cache.GetOrCreate(cacheDek, entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(15)).SetSize(1);
            return InternalDecryptDek(encryptedDek);
        });

        return result ?? throw new InvalidOperationException("DEK cannot be decrypted.");
    }

    private string InternalDecryptDek(string encryptedDek)
    {
        var kek = GetKek();
        var encrypted = Convert.FromBase64String(encryptedDek);
        var dek = AesGcmDecryptBytes(encrypted, kek);

        return Convert.ToBase64String(dek);
    }

    public string Encrypt(string plaintext, string plaintextDek)
    {
        var dek = Convert.FromBase64String(plaintextDek);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = AesGcmEncryptBytes(plainBytes, dek);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string ciphertext, string plaintextDek)
    {
        var dek = Convert.FromBase64String(plaintextDek);
        var encrypted = Convert.FromBase64String(ciphertext);
        var plainBytes = AesGcmDecryptBytes(encrypted, dek);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public (byte[] EncryptedContent, string Iv) EncryptBytes(byte[] plainContent, string plaintextDek)
    {
        var dek = Convert.FromBase64String(plaintextDek);
        var nonce = RandomNumberGenerator.GetBytes(GcmNonceSize);
        var ciphertext = new byte[plainContent.Length];
        var tag = new byte[GcmTagSize];

        using var aes = new AesGcm(dek, GcmTagSize);
        aes.Encrypt(nonce, plainContent, ciphertext, tag);

        var result = new byte[GcmNonceSize + GcmTagSize + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, GcmNonceSize);
        ciphertext.CopyTo(result, GcmNonceSize + GcmTagSize);

        return (result, Convert.ToBase64String(nonce));
    }

    public byte[] DecryptBytes(byte[] encryptedContent, string iv, string plaintextDek)
    {
        var dek = Convert.FromBase64String(plaintextDek);
        var nonce = Convert.FromBase64String(iv);

        var tag = encryptedContent[GcmNonceSize..(GcmNonceSize + GcmTagSize)];
        var ciphertext = encryptedContent[(GcmNonceSize + GcmTagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(dek, GcmTagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    public string ComputeBlindIndexHash(string plaintext)
    {
        var keyBytes = GetSaltIndexHash();
        var dataBytes = Encoding.UTF8.GetBytes(plaintext.Trim().ToLowerInvariant());

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hash);
    }

    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool VerifyPassword(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);

    private byte[] GetKek()
    {
        if (_cachedKek != null) return _cachedKek;

        var base64Kek = _vaultKeyProvider.GetMasterKeyAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        _cachedKek = Convert.FromBase64String(base64Kek);

        return _cachedKek;
    }

    private byte[] GetSaltIndexHash()
    {
        if (_cachedSaltIndexHash != null) return _cachedSaltIndexHash;

        var base64SaltIndexHash = _vaultKeyProvider.GetBlindIndexSecretAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        _cachedSaltIndexHash = Convert.FromBase64String(base64SaltIndexHash);

        return _cachedSaltIndexHash;
    }

    private static byte[] AesGcmEncryptBytes(byte[] plaintext, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(GcmNonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[GcmTagSize];

        using var aes = new AesGcm(key, GcmTagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[GcmNonceSize + GcmTagSize + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, GcmNonceSize);
        ciphertext.CopyTo(result, GcmNonceSize + GcmTagSize);

        return result;
    }

    private static byte[] AesGcmDecryptBytes(byte[] encrypted, byte[] key)
    {
        var nonce = encrypted[..GcmNonceSize];
        var tag = encrypted[GcmNonceSize..(GcmNonceSize + GcmTagSize)];
        var ciphertext = encrypted[(GcmNonceSize + GcmTagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, GcmTagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}