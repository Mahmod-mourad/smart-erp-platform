using System.Text.Json;
using FluentValidation;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Application.Validators;

public class CreateWorkflowRuleDtoValidator : AbstractValidator<CreateWorkflowRuleDto>
{
    public CreateWorkflowRuleDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.TriggerType)
            .Must(t => Enum.TryParse<TriggerType>(t, out _))
            .WithMessage($"TriggerType must be one of: {string.Join(", ", Enum.GetNames<TriggerType>())}.");
        RuleFor(x => x.TriggerConfig).NotEmpty().Must(BeValidJsonObject)
            .WithMessage("TriggerConfig must be a valid JSON object.");
        RuleFor(x => x.ActionsConfig).NotEmpty().Must(BeNonEmptyJsonArray)
            .WithMessage("ActionsConfig must be a non-empty JSON array of actions.");
    }

    internal static bool BeValidJsonObject(string json)
    {
        try { return JsonDocument.Parse(json).RootElement.ValueKind == JsonValueKind.Object; }
        catch (JsonException) { return false; }
    }

    internal static bool BeNonEmptyJsonArray(string json)
    {
        try
        {
            var root = JsonDocument.Parse(json).RootElement;
            return root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0;
        }
        catch (JsonException) { return false; }
    }
}

public class UpdateWorkflowRuleDtoValidator : AbstractValidator<UpdateWorkflowRuleDto>
{
    public UpdateWorkflowRuleDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.TriggerType)
            .Must(t => Enum.TryParse<TriggerType>(t, out _))
            .WithMessage($"TriggerType must be one of: {string.Join(", ", Enum.GetNames<TriggerType>())}.");
        RuleFor(x => x.TriggerConfig).NotEmpty()
            .Must(CreateWorkflowRuleDtoValidator.BeValidJsonObject)
            .WithMessage("TriggerConfig must be a valid JSON object.");
        RuleFor(x => x.ActionsConfig).NotEmpty()
            .Must(CreateWorkflowRuleDtoValidator.BeNonEmptyJsonArray)
            .WithMessage("ActionsConfig must be a non-empty JSON array of actions.");
    }
}
