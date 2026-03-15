using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Entities;

namespace LinkVault.Application.Mappings;

public static class UserMappingExtensions
{
    public static UserDto ToDto(
        this UserEntity user,
        IEncryptionService encryptionService)
    {
        var plaintextDek = encryptionService.DecryptDek(user.EncryptedDek);

        return new UserDto(
            Email: encryptionService.Decrypt(user.EmailEncrypted, plaintextDek),
            FirstName: encryptionService.Decrypt(user.FirstNameEncrypted, plaintextDek),
            Surname: encryptionService.Decrypt(user.SurNameEncrypted, plaintextDek),
            Tier: "Free", 
            CreatedAt: user.CreatedAt,
            Role: user.IsAdmin ? "Admin" : "User"
        );
    }
}