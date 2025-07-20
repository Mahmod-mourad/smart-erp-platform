using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

public class JournalEntryLine : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    
    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}
