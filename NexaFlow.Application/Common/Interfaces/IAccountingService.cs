using NexaFlow.Application.DTOs;

namespace NexaFlow.Application.Common.Interfaces;

public interface IAccountingService
{
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken ct = default);
    Task<AccountDto> CreateAccountAsync(CreateAccountDto request, CancellationToken ct = default);
    Task<JournalEntryDto> PostJournalEntryAsync(CreateJournalEntryDto request, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(CancellationToken ct = default);
}
