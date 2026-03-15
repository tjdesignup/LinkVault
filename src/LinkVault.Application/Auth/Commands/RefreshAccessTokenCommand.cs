using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public record RefreshAccessTokenCommand(
    string RefreshToken,
    string IpAddress,
    string DeviceName
) : IRequest<string>;