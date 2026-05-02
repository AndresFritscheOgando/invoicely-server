namespace Invoicely.Core.DTOs;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
