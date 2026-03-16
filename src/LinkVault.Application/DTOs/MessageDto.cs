namespace LinkVault.Application.DTOs;
public record MessageDto(
    string Message,
    string? RedirectUrl = null, 
    bool Success = true
);