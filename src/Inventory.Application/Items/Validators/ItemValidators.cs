using FluentValidation;
using Inventory.Application.Items.Dtos;

namespace Inventory.Application.Items.Validators;

public class CreateItemValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters")
            .Matches("^[A-Za-z0-9-_]+$").WithMessage("SKU can only contain letters, numbers, hyphens, and underscores");
            
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
            
        RuleFor(x => x.UnitOfMeasureId)
            .NotEmpty().WithMessage("Unit of Measure is required");
            
        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be zero or positive");
            
        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Sale price must be zero or positive");
            
        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).When(x => x.MinimumStock.HasValue)
            .WithMessage("Minimum stock must be zero or positive");
    }
}

public class UpdateItemValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
            
        RuleFor(x => x.UnitOfMeasureId)
            .NotEmpty().WithMessage("Unit of Measure is required");
            
        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be zero or positive");
            
        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Sale price must be zero or positive");
            
        RuleFor(x => x.RowVersion)
            .GreaterThan(0).WithMessage("RowVersion must be provided for updates");
    }
}
