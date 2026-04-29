namespace Invoicely.Core.DTOs;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string Role = "Employee"
);
