using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

public class Account : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    
    public string Code { get; set; } = null!; // e.g., "1000", "1010"
    public string Name { get; set; } = null!;
    public AccountType Type { get; set; }
    
    public Guid? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }
    public ICollection<Account> ChildAccounts { get; set; } = new List<Account>();
    
    // Derived or cached balance based on debits/credits, or calculated on the fly
    public decimal Balance { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
