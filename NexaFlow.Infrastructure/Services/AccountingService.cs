using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class AccountingService(AppDbContext db, ICurrentUser currentUser) : IAccountingService
{
    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await db.Accounts.AsNoTracking().ToListAsync(ct);
        return accounts.Select(a => new AccountDto(a.Id, a.Code, a.Name, a.Type, a.ParentAccountId, a.Balance)).ToList();
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto request, CancellationToken ct = default)
    {
        if (await db.Accounts.AnyAsync(a => a.Code == request.Code, ct))
            throw new ConflictException("Account code already exists.");

        if (request.ParentAccountId.HasValue)
        {
            var parent = await db.Accounts.FindAsync([request.ParentAccountId.Value], ct)
                         ?? throw new NotFoundException("Parent account not found.");
            
            if (parent.Type != request.Type)
                throw new ValidationException(new Dictionary<string, string[]> { { "Type", ["Child account must have same type as parent."] } });
        }

        var account = new Account
        {
            Code = request.Code,
            Name = request.Name,
            Type = request.Type,
            ParentAccountId = request.ParentAccountId,
            Balance = 0
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        return new AccountDto(account.Id, account.Code, account.Name, account.Type, account.ParentAccountId, account.Balance);
    }

    public async Task<JournalEntryDto> PostJournalEntryAsync(CreateJournalEntryDto request, CancellationToken ct = default)
    {
        if (await db.JournalEntries.AnyAsync(j => j.ReferenceNumber == request.ReferenceNumber, ct))
            throw new ConflictException("Reference number already exists.");

        var entry = new JournalEntry
        {
            ReferenceNumber = request.ReferenceNumber,
            Date = request.Date,
            Description = request.Description,
            Status = JournalEntryStatus.Posted,
            CreatedByUserId = currentUser.UserId == Guid.Empty ? null : currentUser.UserId
        };

        foreach (var lineReq in request.Lines)
        {
            var account = await db.Accounts.FindAsync([lineReq.AccountId], ct)
                          ?? throw new NotFoundException($"Account {lineReq.AccountId} not found.");

            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = account.Id,
                Debit = lineReq.Debit,
                Credit = lineReq.Credit,
                Description = lineReq.Description
            });

            // Update Account Balance
            // Asset/Expense: +Debit, -Credit
            // Liability/Equity/Revenue: +Credit, -Debit
            if (account.Type == AccountType.Asset || account.Type == AccountType.Expense)
            {
                account.Balance += lineReq.Debit - lineReq.Credit;
            }
            else
            {
                account.Balance += lineReq.Credit - lineReq.Debit;
            }
        }

        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        var lineDtos = entry.Lines.Select(l => new JournalEntryLineDto(l.AccountId, l.Debit, l.Credit, l.Description)).ToList();
        return new JournalEntryDto(entry.Id, entry.ReferenceNumber, entry.Date, entry.Description, entry.Status, lineDtos);
    }

    public async Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(CancellationToken ct = default)
    {
        var entries = await db.JournalEntries
            .Include(j => j.Lines)
            .AsNoTracking()
            .OrderByDescending(j => j.Date)
            .ToListAsync(ct);

        return entries.Select(j => new JournalEntryDto(
            j.Id, j.ReferenceNumber, j.Date, j.Description, j.Status,
            j.Lines.Select(l => new JournalEntryLineDto(l.AccountId, l.Debit, l.Credit, l.Description)).ToList()
        )).ToList();
    }
}
