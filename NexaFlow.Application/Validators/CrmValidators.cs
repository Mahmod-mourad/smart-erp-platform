using FluentValidation;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Application.Validators;

public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Company).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Company).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<CustomerStatus>(s, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<CustomerStatus>())}.");
    }
}

public class CreateLeadDtoValidator : AbstractValidator<CreateLeadDto>
{
    public CreateLeadDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public class UpdateLeadStageDtoValidator : AbstractValidator<UpdateLeadStageDto>
{
    public UpdateLeadStageDtoValidator()
    {
        RuleFor(x => x.Stage)
            .Must(s => Enum.TryParse<LeadStage>(s, out _))
            .WithMessage($"Stage must be one of: {string.Join(", ", Enum.GetNames<LeadStage>())}.");
    }
}

public class CreateActivityDtoValidator : AbstractValidator<CreateActivityDto>
{
    public CreateActivityDtoValidator()
    {
        // System-generated StatusChange entries are written directly by the services, never
        // through this endpoint, so users may only log the manual types.
        RuleFor(x => x.Type)
            .Must(t => Enum.TryParse<ActivityType>(t, out var parsed) && parsed != ActivityType.StatusChange)
            .WithMessage("Type must be one of: Note, Call, Email, Meeting.");
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}
