using Accounting.Application.Items.DTOs;
using FluentValidation;

namespace Accounting.Application.Common.Validators;

public class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(300).WithMessage("Item name must not exceed 300 characters.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100).WithMessage("SKU must not exceed 100 characters.")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("SKU may only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).When(x => x.Barcode is not null);

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Unit of measure is required.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be zero or positive.");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Sale price must be zero or positive.");

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder level must be zero or positive.");

        RuleFor(x => x.MinExpiryDaysBeforeSale)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum expiry days must be zero or positive.");

        // Business rule: expiry tracking requires batch tracking
        RuleFor(x => x.TrackBatch)
            .Equal(true)
            .When(x => x.TrackExpiry)
            .WithMessage("Batch tracking must be enabled when expiry tracking is enabled.");
    }
}

public class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(300);

        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinExpiryDaysBeforeSale).GreaterThanOrEqualTo(0);

        RuleFor(x => x.TrackBatch)
            .Equal(true)
            .When(x => x.TrackExpiry)
            .WithMessage("Batch tracking must be enabled when expiry tracking is enabled.");
    }
}

