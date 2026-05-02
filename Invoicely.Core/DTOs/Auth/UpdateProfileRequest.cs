namespace Invoicely.Core.DTOs;

public record UpdateProfileRequest(
    string Name,
    string Email
);
