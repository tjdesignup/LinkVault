using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Account.Commands;

public record UpdateProfileCommand(
    string FirstName,
    string Surname
) : IRequest<UserDto>;