using FluentValidation;
using Invoicely.Core.DTOs;
using Invoicely.Core.Enums;

namespace Invoicely.Api.Validators;

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    private static readonly string[] ValidMethods = Enum.GetNames<PaymentMethod>();

    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0.");
        RuleFor(x => x.PaymentDate)
            .NotEmpty()
            .Matches(@"^\d{4}-\d{2}-\d{2}$")
            .WithMessage("Payment date must be YYYY-MM-DD.");
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => ValidMethods.Contains(m))
            .WithMessage($"Payment method must be one of: {string.Join(", ", ValidMethods)}.");
    }
}
