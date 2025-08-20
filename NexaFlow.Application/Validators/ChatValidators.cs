using FluentValidation;
using NexaFlow.Application.DTOs;

namespace NexaFlow.Application.Validators;

public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000)
            .WithMessage("Message must be between 1 and 2000 characters.");
        RuleFor(x => x.History).NotNull();
    }
}
