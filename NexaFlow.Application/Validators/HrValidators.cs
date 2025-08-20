using FluentValidation;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Application.Validators;

public class CreateEmployeeDtoValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.NationalId).MaximumLength(40);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Allowances).GreaterThanOrEqualTo(0);
    }
}

public class UpdateEmployeeDtoValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Allowances).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<EmployeeStatus>(s, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<EmployeeStatus>())}.");
    }
}

public class CreateLeaveRequestDtoValidator : AbstractValidator<CreateLeaveRequestDto>
{
    public CreateLeaveRequestDtoValidator()
    {
        RuleFor(x => x.Type)
            .Must(t => Enum.TryParse<LeaveType>(t, out _))
            .WithMessage($"Type must be one of: {string.Join(", ", Enum.GetNames<LeaveType>())}.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date.");
    }
}

public class ReviewLeaveDtoValidator : AbstractValidator<ReviewLeaveDto>
{
    public ReviewLeaveDtoValidator()
    {
        RuleFor(x => x.ReviewNote).MaximumLength(1000);
    }
}
