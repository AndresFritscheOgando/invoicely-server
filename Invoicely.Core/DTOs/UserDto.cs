namespace Invoicely.Core.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);
