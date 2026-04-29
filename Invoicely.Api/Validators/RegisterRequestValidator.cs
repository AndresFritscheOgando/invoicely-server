using FluentValidation;
using Invoicely.Core.DTOs;
using Invoicely.Core.Enums;

namespace Invoicely.Api.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.Role)
            .Must(r => Enum.TryParse<UserRole>(r, out _))
            .WithMessage("Role must be Admin, FinanceManager, Employee, or Viewer.");
    }
}
