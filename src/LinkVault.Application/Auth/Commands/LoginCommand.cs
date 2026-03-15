using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string DeviceName,
    string IpAddress
) : IRequest<AuthResultDto>;