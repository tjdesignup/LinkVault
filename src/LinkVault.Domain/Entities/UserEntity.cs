using System.Reflection.Metadata;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Exceptions;

namespace LinkVault.Domain.Entities;

public class UserEntity : BaseEntity
{
    public string EmailEncrypted { get; private set; } = string.Empty;
    public string EmailBlindIndexHash { get; private set; } = string.Empty;
    public string FirstNameEncrypted { get; private set; } = string.Empty;
    public string SurNameEncrypted { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string EncryptedDek { get; private set; } = string.Empty;
    public DateTime? LastLogin {get; private set;}
    public DateTime? UpdatedAt {get; private set;} 
    public bool EmailConfirmed { get; private set; }
    public bool IsAdmin { get; private set; } = false;
    public string? PendingEmailEncrypted { get; private set; }
    public string? PendingEmailBlindIndexHash { get; private set; }
    private UserEntity() { }

    private UserEntity(
        string emailEncrypted,
        string emailBlindIndexHash,
        string firstNameEncrypted,
        string surNameEncrypted,
        string passwordHash,
        string encryptedDek)
    {
        EmailEncrypted = emailEncrypted;
        EmailBlindIndexHash = emailBlindIndexHash;
        FirstNameEncrypted = firstNameEncrypted;
        SurNameEncrypted = surNameEncrypted;
        PasswordHash = passwordHash;
        EncryptedDek = encryptedDek;
    }

    public static UserEntity Register(
        string emailEncrypted,
        string emailBlindIndex,
        string firstNameEncrypted,
        string surNameEncrypted,
        string passwordHash,
        string encryptedDek)
    {
        if (string.IsNullOrWhiteSpace(emailEncrypted))
            throw new ArgumentException("Encrypted email cannot be empty.", nameof(emailEncrypted));

        if (string.IsNullOrWhiteSpace(emailBlindIndex))
            throw new ArgumentException("Email blind index cannot be empty.", nameof(emailBlindIndex));

        if (string.IsNullOrWhiteSpace(firstNameEncrypted))
            throw new ArgumentException("Encrypted display name cannot be empty.", nameof(firstNameEncrypted));
        
        if (string.IsNullOrWhiteSpace(surNameEncrypted))
            throw new ArgumentException("Encrypted display name cannot be empty.", nameof(surNameEncrypted));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(encryptedDek))
            throw new ArgumentException("Encrypted DEK cannot be empty.", nameof(encryptedDek));

        return new UserEntity(emailEncrypted, emailBlindIndex, firstNameEncrypted,surNameEncrypted, passwordHash, encryptedDek);
    }

    public void ConfirmEmail()
    {
        if (EmailConfirmed)
            throw new EmailAlreadyConfirmedException();

        EmailConfirmed = true;
    }

    public void RequestEmailChange(string newEmailEncrypted, string newEmailBlindIndexHash)
    {
        if (string.IsNullOrWhiteSpace(newEmailEncrypted))
            throw new ArgumentException("Encrypted email cannot be empty.", nameof(newEmailEncrypted));

        if (string.IsNullOrWhiteSpace(newEmailBlindIndexHash))
            throw new ArgumentException("Email blind index cannot be empty.", nameof(newEmailBlindIndexHash));

        if (newEmailBlindIndexHash == EmailBlindIndexHash)
            throw new ArgumentException("New email must be different from the current email.", nameof(newEmailBlindIndexHash));

        PendingEmailEncrypted = newEmailEncrypted;
        PendingEmailBlindIndexHash = newEmailBlindIndexHash;
    }
    
    public void ConfirmEmailChange()
    {
        if (PendingEmailEncrypted is null || PendingEmailBlindIndexHash is null)
            throw new InvalidOperationException("No pending email change exists.");

        EmailEncrypted = PendingEmailEncrypted;
        EmailBlindIndexHash = PendingEmailBlindIndexHash;
        PendingEmailEncrypted = null;
        PendingEmailBlindIndexHash = null;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newPasswordHash));

        if (newPasswordHash == PasswordHash)
            throw new SamePasswordException();

        PasswordHash = newPasswordHash;
    }

    public void UpdateProfile(string firstNameEncrypted, string surNameEncrypted)
    {
        if (string.IsNullOrWhiteSpace(firstNameEncrypted))
            throw new ArgumentException("Encrypted display name cannot be empty.", nameof(firstNameEncrypted));

        if (string.IsNullOrWhiteSpace(surNameEncrypted))
            throw new ArgumentException("Encrypted display name cannot be empty.", nameof(surNameEncrypted));

        FirstNameEncrypted = firstNameEncrypted;
        SurNameEncrypted = surNameEncrypted;
        Update();
    }

    public void Update() => UpdatedAt = DateTime.UtcNow;

    public void Delete() => SoftDelete();
}