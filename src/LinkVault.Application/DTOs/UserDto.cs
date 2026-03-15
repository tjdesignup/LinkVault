namespace LinkVault.Application.DTOs;

public record UserDto(
    string Email,         
    string FirstName,     
    string Surname,         
    string Tier,      
    DateTime CreatedAt,
    string Role
);