using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Application.Mappings;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Account.Commands;

public class UpdateProfileHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateProfileCommand, UserDto>
{
    public async Task<UserDto> Handle(
        UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new ResourceNotFoundException("User", currentUser.UserId);

        var plaintextDek = encryptionService.DecryptDek(user.EncryptedDek);
        var firstNameEncrypted = encryptionService.Encrypt(command.FirstName, plaintextDek);
        var surnameEncrypted = encryptionService.Encrypt(command.Surname, plaintextDek);

        user.UpdateProfile(firstNameEncrypted, surnameEncrypted);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return user.ToDto(encryptionService);
    }
}