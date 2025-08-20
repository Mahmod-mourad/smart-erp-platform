using FluentValidation;
using NexaFlow.Core.Enums;

namespace NexaFlow.Application.DTOs;

public record AccountDto(Guid Id, string Code, string Name, AccountType Type, Guid? ParentAccountId, decimal Balance);

public record CreateAccountDto(string Code, string Name, AccountType Type, Guid? ParentAccountId);

public class CreateAccountDtoValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountDtoValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).IsInEnum();
    }
}

public record JournalEntryLineDto(Guid AccountId, decimal Debit, decimal Credit, string? Description);

public record JournalEntryDto(Guid Id, string ReferenceNumber, DateTime Date, string Description, JournalEntryStatus Status, IReadOnlyList<JournalEntryLineDto> Lines);

public record CreateJournalEntryDto(string ReferenceNumber, DateTime Date, string Description, IReadOnlyList<JournalEntryLineDto> Lines);

public class CreateJournalEntryDtoValidator : AbstractValidator<CreateJournalEntryDto>
{
    public CreateJournalEntryDtoValidator()
    {
        RuleFor(x => x.ReferenceNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().Must(lines => lines.Count >= 2).WithMessage("At least two lines are required.");
        
        RuleFor(x => x).Must(x =>
        {
            var totalDebit = x.Lines?.Sum(l => l.Debit) ?? 0;
            var totalCredit = x.Lines?.Sum(l => l.Credit) ?? 0;
            return totalDebit == totalCredit && totalDebit > 0;
        }).WithMessage("Total Debits must equal Total Credits and must be greater than zero.");
    }
}
