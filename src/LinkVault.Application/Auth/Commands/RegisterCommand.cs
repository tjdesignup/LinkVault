using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string Surname
) : IRequest<MessageDto>;