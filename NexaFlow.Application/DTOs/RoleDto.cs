using FluentValidation;

namespace NexaFlow.Application.DTOs;

public record RoleDto(Guid Id, string Name, string? Description, IReadOnlyList<string> Permissions);

public record CreateRoleDto(string Name, string? Description, IReadOnlyList<string> Permissions);

public record UpdateRoleDto(string Name, string? Description, IReadOnlyList<string> Permissions);

public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
{
    public CreateRoleDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Permissions).NotNull();
    }
}

public class UpdateRoleDtoValidator : AbstractValidator<UpdateRoleDto>
{
    public UpdateRoleDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Permissions).NotNull();
    }
}
