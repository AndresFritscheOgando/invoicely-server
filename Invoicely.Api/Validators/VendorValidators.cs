using FluentValidation;
using Invoicely.Core.DTOs;
using Invoicely.Core.Enums;

namespace Invoicely.Api.Validators;

public class CreateVendorRequestValidator : AbstractValidator<CreateVendorRequest>
{
    public CreateVendorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone != null);
        RuleFor(x => x.TaxId).MaximumLength(100).When(x => x.TaxId != null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
    }
}

public class UpdateVendorRequestValidator : AbstractValidator<UpdateVendorRequest>
{
    public UpdateVendorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone != null);
        RuleFor(x => x.TaxId).MaximumLength(100).When(x => x.TaxId != null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
    }
}

public class UpdateVendorStatusRequestValidator : AbstractValidator<UpdateVendorStatusRequest>
{
    private static readonly string[] ValidStatuses = Enum.GetNames<VendorStatus>();

    public UpdateVendorStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");
    }
}
