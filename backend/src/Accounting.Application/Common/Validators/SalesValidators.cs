using Accounting.Application.Sales.DTOs;
using FluentValidation;

namespace Accounting.Application.Common.Validators;

public class CreateSalesInvoiceRequestValidator : AbstractValidator<CreateSalesInvoiceRequest>
{
    public CreateSalesInvoiceRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().WithMessage("Branch is required.");
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("Warehouse is required.");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Paid amount must be zero or positive.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("At least one sale line is required.")
            .Must(lines => lines.Count <= 200).WithMessage("A single sale cannot have more than 200 lines.");

        RuleForEach(x => x.Lines).SetValidator(new CreateSalesInvoiceLineRequestValidator());
    }
}

public class CreateSalesInvoiceLineRequestValidator : AbstractValidator<CreateSalesInvoiceLineRequest>
{
    public CreateSalesInvoiceLineRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("Item is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be zero or positive.");

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("Discount percent must be between 0 and 100.");

        RuleFor(x => x.TaxPercent)
            .InclusiveBetween(0, 100).WithMessage("Tax percent must be between 0 and 100.");
    }
}

