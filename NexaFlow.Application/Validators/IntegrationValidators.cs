using FluentValidation;
using NexaFlow.Application.DTOs;

namespace NexaFlow.Application.Validators;

public class UpsertIntegrationDtoValidator : AbstractValidator<UpsertIntegrationDto>
{
    public UpsertIntegrationDtoValidator()
    {
        RuleFor(x => x.Config).NotNull().WithMessage("Config is required.");
    }
}
