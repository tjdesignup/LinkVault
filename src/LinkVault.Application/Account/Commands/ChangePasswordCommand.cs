using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Account.Commands;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string NewPasswordConfirmation
) : IRequest<MessageDto>;