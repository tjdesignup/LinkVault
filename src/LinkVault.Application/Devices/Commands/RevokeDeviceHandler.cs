using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Devices.Commands;

public class RevokeDeviceHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeDeviceCommand, MessageDto>
{
    public async Task<MessageDto> Handle(
        RevokeDeviceCommand command,
        CancellationToken cancellationToken)
    {
        var token = await refreshTokenRepository.FindByIdAsync(
            command.DeviceId, cancellationToken)
            ?? throw new ResourceNotFoundException("Device", command.DeviceId);

        if (token.UserId != currentUser.UserId)
            throw new ResourceForbiddenException("Device");

        token.Revoke();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageDto("Device revoked successfully.");
    }
}