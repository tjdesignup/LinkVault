namespace LinkVault.Application.DTOs;

public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    UserDto User
);