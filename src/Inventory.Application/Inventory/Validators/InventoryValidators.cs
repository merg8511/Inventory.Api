using FluentValidation;
using Inventory.Application.Inventory.Dtos;

namespace Inventory.Application.Inventory.Validators;

public class ReceiptRequestValidator : AbstractValidator<ReceiptRequest>
{
    public ReceiptRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("Item ID is required");
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("Warehouse ID is required");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive");
        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitCost.HasValue)
            .WithMessage("Unit cost must be zero or positive");
    }
}

public class IssueRequestValidator : AbstractValidator<IssueRequest>
{
    public IssueRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("Item ID is required");
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("Warehouse ID is required");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive");
    }
}

public class AdjustmentRequestValidator : AbstractValidator<AdjustmentRequest>
{
    public AdjustmentRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("Item ID is required");
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("Warehouse ID is required");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive");
        RuleFor(x => x.AdjustmentType)
            .NotEmpty().WithMessage("Adjustment type is required")
            .Must(x => x.Equals("increase", StringComparison.OrdinalIgnoreCase) || 
                       x.Equals("decrease", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Adjustment type must be 'increase' or 'decrease'");
        RuleFor(x => x.ReasonCode)
            .NotEmpty().WithMessage("Reason code is required for adjustments");
    }
}
