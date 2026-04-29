using FluentValidation;
using Invoicely.Core.DTOs;

namespace Invoicely.Api.Validators;

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty().WithMessage("Vendor is required.");
        RuleFor(x => x.IssueDate).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Issue date must be YYYY-MM-DD.");
        RuleFor(x => x.DueDate).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Due date must be YYYY-MM-DD.");
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10).WithMessage("Currency is required.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Items).SetValidator(new InvoiceItemRequestValidator());
    }
}

public class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty().WithMessage("Vendor is required.");
        RuleFor(x => x.IssueDate).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Issue date must be YYYY-MM-DD.");
        RuleFor(x => x.DueDate).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Due date must be YYYY-MM-DD.");
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10).WithMessage("Currency is required.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Items).SetValidator(new InvoiceItemRequestValidator());
    }
}

public class InvoiceItemRequestValidator : AbstractValidator<InvoiceItemRequest>
{
    public InvoiceItemRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500).WithMessage("Item description is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        RuleFor(x => x.UnitPrice).GreaterThan(0).WithMessage("Unit price must be greater than 0.");
    }
}
