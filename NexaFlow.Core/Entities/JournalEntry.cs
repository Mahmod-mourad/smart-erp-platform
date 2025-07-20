using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

public class JournalEntry : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    
    public string ReferenceNumber { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Description { get; set; } = null!;
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
}
