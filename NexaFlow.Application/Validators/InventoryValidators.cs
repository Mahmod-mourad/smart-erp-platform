using FluentValidation;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Application.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SKU).MaximumLength(60);
        RuleFor(x => x.Category).MaximumLength(80);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class AddStockMovementDtoValidator : AbstractValidator<AddStockMovementDto>
{
    public AddStockMovementDtoValidator()
    {
        RuleFor(x => x.Type)
            .Must(t => Enum.TryParse<StockMovementType>(t, out _))
            .WithMessage($"Type must be one of: {string.Join(", ", Enum.GetNames<StockMovementType>())}.");
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
