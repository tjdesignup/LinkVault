namespace LinkVault.Application.Abstractions;

public interface IEncryptionService
{
    string GenerateEncryptedDek();
    string DecryptDek(string encryptedDek);
    string Encrypt(string plaintext, string plaintextDek);
    string Decrypt(string ciphertext, string plaintextDek);
    (byte[] EncryptedContent, string Iv) EncryptBytes(byte[] plainContent, string plaintextDek);
    byte[] DecryptBytes(byte[] encryptedContent, string iv, string plaintextDek);
    string ComputeBlindIndexHash(string plaintext);
    string HashPassword(string plaintextPassword);
    bool VerifyPassword(string password, string passwordHash);
}