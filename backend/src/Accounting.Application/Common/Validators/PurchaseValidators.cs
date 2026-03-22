using Accounting.Application.Purchasing.DTOs;
using FluentValidation;

namespace Accounting.Application.Common.Validators;

public class CreatePurchaseInvoiceRequestValidator : AbstractValidator<CreatePurchaseInvoiceRequest>
{
    public CreatePurchaseInvoiceRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty().WithMessage("Supplier is required.");
        RuleFor(x => x.BranchId).NotEmpty().WithMessage("Branch is required.");
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("Warehouse is required.");
        RuleFor(x => x.InvoiceDate).NotEmpty().WithMessage("Invoice date is required.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.InvoiceDate)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be on or after the invoice date.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("At least one invoice line is required.")
            .Must(lines => lines.Count <= 500).WithMessage("A single invoice cannot have more than 500 lines.");

        RuleForEach(x => x.Lines).SetValidator(new CreatePurchaseInvoiceLineRequestValidator());
    }
}

public class CreatePurchaseInvoiceLineRequestValidator : AbstractValidator<CreatePurchaseInvoiceLineRequest>
{
    public CreatePurchaseInvoiceLineRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("Item is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("Unit cost must be zero or positive.");

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("Discount percent must be between 0 and 100.");

        RuleFor(x => x.TaxPercent)
            .InclusiveBetween(0, 100).WithMessage("Tax percent must be between 0 and 100.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.Today))
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("Expiry date must be in the future.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.ProductionDate)
            .When(x => x.ExpiryDate.HasValue && x.ProductionDate.HasValue)
            .WithMessage("Expiry date must be on or after the production date.");
    }
}

