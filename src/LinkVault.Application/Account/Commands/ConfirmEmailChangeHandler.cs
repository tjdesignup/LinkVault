using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Account.Commands;

public class ConfirmEmailChangeHandler(
    IEmailConfirmationTokenRepository tokenRepository,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmEmailChangeCommand, string>
{
    public async Task<string> Handle(
        ConfirmEmailChangeCommand command,
        CancellationToken cancellationToken)
    {
        var token = await tokenRepository.FindByTokenAsync(
            command.Token,
            ConfirmationTokenType.EmailChange,
            cancellationToken) ?? throw new InvalidConfirmationTokenException();

        token.Use();

        var user = await userRepository.FindByIdAsync(token.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {token.UserId} not found.");

        user.ConfirmEmailChange();

        await refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Email change confirmed successfully.";
    }
}