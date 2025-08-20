using FluentValidation;
using NexaFlow.Application.DTOs;

namespace NexaFlow.Application.Validators;

public class CreateBranchValidator : AbstractValidator<CreateBranchDto>
{
    public CreateBranchValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(100).WithMessage("Branch name must not exceed 100 characters.");
            
        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City name must not exceed 50 characters.");
            
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");
    }
}

public class UpdateBranchValidator : AbstractValidator<UpdateBranchDto>
{
    public UpdateBranchValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(100).WithMessage("Branch name must not exceed 100 characters.");
            
        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City name must not exceed 50 characters.");
            
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");
    }
}
