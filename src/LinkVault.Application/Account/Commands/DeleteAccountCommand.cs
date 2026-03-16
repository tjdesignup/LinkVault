using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Account.Commands;

public record DeleteAccountCommand(
    string CurrentPassword
) : IRequest<MessageDto>;