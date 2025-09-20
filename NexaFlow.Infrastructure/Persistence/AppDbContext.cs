using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Common;
using NexaFlow.Core.Entities;
using NexaFlow.Infrastructure.Identity;

namespace NexaFlow.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TeamInvitation> Invitations => Set<TeamInvitation>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<WorkflowRule> WorkflowRules => Set<WorkflowRule>();
    public DbSet<WorkflowLog> WorkflowLogs => Set<WorkflowLog>();
    public DbSet<TenantIntegration> TenantIntegrations => Set<TenantIntegration>();
    public DbSet<CustomRole> CustomRoles => Set<CustomRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(150).IsRequired();
            e.Property(t => t.Slug).HasMaxLength(80).IsRequired();
            e.HasIndex(t => t.Slug).IsUnique();
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100);
            e.Property(u => u.LastName).HasMaxLength(100);
            e.HasIndex(u => u.TenantId);
            e.HasOne(u => u.CustomRole)
                .WithMany()
                .HasForeignKey(u => u.CustomRoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.Property(t => t.Token).HasMaxLength(200).IsRequired();
            e.HasIndex(t => t.Token).IsUnique();
            e.HasIndex(t => t.UserId);
        });

        builder.Entity<TeamInvitation>(e =>
        {
            e.Property(i => i.Email).HasMaxLength(256).IsRequired();
            e.Property(i => i.RoleName).HasMaxLength(50).IsRequired();
            e.Property(i => i.Token).HasMaxLength(200).IsRequired();
            e.HasIndex(i => i.Token).IsUnique();
            e.HasIndex(i => new { i.TenantId, i.Email });
        });

        builder.Entity<Customer>(e =>
        {
            e.ToTable("Customers", b => b.IsTemporal());
            e.Property(c => c.Name).HasMaxLength(150).IsRequired();
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.Phone).HasMaxLength(40);
            e.Property(c => c.Company).HasMaxLength(150);
            e.Property(c => c.Notes).HasMaxLength(2000);
            e.HasIndex(c => c.TenantId);
            e.HasMany(c => c.Leads)
                .WithOne(l => l.Customer!)
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Activities)
                .WithOne(a => a.Customer!)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Lead>(e =>
        {
            e.Property(l => l.Title).HasMaxLength(150).IsRequired();
            e.Property(l => l.Value).HasPrecision(18, 2);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => l.CustomerId);
        });

        builder.Entity<Employee>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.Property(x => x.NationalId).HasMaxLength(40);
            e.Property(x => x.Department).HasMaxLength(100).IsRequired();
            e.Property(x => x.Position).HasMaxLength(100).IsRequired();
            e.Property(x => x.BaseSalary).HasPrecision(18, 2);
            e.Property(x => x.Allowances).HasPrecision(18, 2);
            e.Ignore(x => x.FullName);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Branch)
                .WithMany(b => b.Employees)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Branch>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.City).HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasIndex(x => x.TenantId);
        });

        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.EmployeeId, x.Date });
            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.AttendanceRecords)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LeaveRequest>(e =>
        {
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.ReviewNote).HasMaxLength(1000);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.EmployeeId);
            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.LeaveRequests)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Product>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.SKU).HasMaxLength(60);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(x => x.TenantId);
            e.HasMany(x => x.Movements)
                .WithOne(m => m.Product)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StockMovement>(e =>
        {
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.ProductId);
        });

        builder.Entity<Activity>(e =>
        {
            e.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.CustomerId);
        });

        builder.Entity<WorkflowRule>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.TriggerConfig).IsRequired();
            e.Property(x => x.ActionsConfig).IsRequired();
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.IsActive });
            e.HasMany(x => x.Logs)
                .WithOne(l => l.Rule)
                .HasForeignKey(l => l.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkflowLog>(e =>
        {
            e.Property(x => x.Details).IsRequired();
            e.Property(x => x.TriggerData).HasMaxLength(2000);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.RuleId, x.ExecutedAt });
        });

        builder.Entity<TenantIntegration>(e =>
        {
            e.Property(x => x.EncryptedConfig).IsRequired();
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.Type }).IsUnique();
        });

        builder.Entity<CustomRole>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.TenantId);
            e.HasMany(x => x.Permissions)
                .WithOne(p => p.CustomRole)
                .HasForeignKey(p => p.CustomRoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RolePermission>(e =>
        {
            e.Property(x => x.Permission).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.CustomRoleId, x.Permission }).IsUnique();
        });

        builder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.EntityName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(150).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId });
            e.HasIndex(x => x.Timestamp);
        });

        builder.Entity<Account>(e =>
        {
            e.ToTable("Accounts", b => b.IsTemporal());
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Balance).HasPrecision(18, 4);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.HasOne(x => x.ParentAccount)
                .WithMany(p => p.ChildAccounts)
                .HasForeignKey(x => x.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JournalEntry>(e =>
        {
            e.ToTable("JournalEntries", b => b.IsTemporal());
            e.Property(x => x.ReferenceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.ReferenceNumber }).IsUnique();
            e.HasIndex(x => x.Date);
        });

        builder.Entity<JournalEntryLine>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Debit).HasPrecision(18, 4);
            e.Property(x => x.Credit).HasPrecision(18, 4);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.JournalEntry)
                .WithMany(j => j.Lines)
                .HasForeignKey(x => x.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserPreferences>(e =>
        {
            e.Property(x => x.PrimaryColor).HasMaxLength(20);
            e.Property(x => x.SecondaryColor).HasMaxLength(20);
            e.Property(x => x.Language).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        ApplyTenantQueryFilters(builder);
    }

    /// <summary>
    /// Global query filter: every ITenantEntity read is automatically scoped to the
    /// current tenant. The filter reads the live <see cref="ITenantContext"/> instance,
    /// so it stays correct per scoped request (T-004).
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder builder)
    {
        // When no tenant is set (anonymous endpoints: login / register / accept-invite, or a
        // platform SuperAdmin) the filter is bypassed so cross-tenant lookups can resolve.
        // For every authenticated request the JWT sets TenantId, so isolation is enforced.
        builder.Entity<ApplicationUser>()
            .HasQueryFilter(u => !_tenantContext.TenantId.HasValue || u.TenantId == _tenantContext.TenantId);
        builder.Entity<TeamInvitation>()
            .HasQueryFilter(i => !_tenantContext.TenantId.HasValue || i.TenantId == _tenantContext.TenantId);
        builder.Entity<Customer>()
            .HasQueryFilter(c => !_tenantContext.TenantId.HasValue || c.TenantId == _tenantContext.TenantId);
        builder.Entity<Lead>()
            .HasQueryFilter(l => !_tenantContext.TenantId.HasValue || l.TenantId == _tenantContext.TenantId);
        builder.Entity<Employee>()
            .HasQueryFilter(e => !_tenantContext.TenantId.HasValue || e.TenantId == _tenantContext.TenantId);
        builder.Entity<Branch>()
            .HasQueryFilter(b => !_tenantContext.TenantId.HasValue || b.TenantId == _tenantContext.TenantId);
        builder.Entity<AttendanceRecord>()
            .HasQueryFilter(a => !_tenantContext.TenantId.HasValue || a.TenantId == _tenantContext.TenantId);
        builder.Entity<LeaveRequest>()
            .HasQueryFilter(l => !_tenantContext.TenantId.HasValue || l.TenantId == _tenantContext.TenantId);
        builder.Entity<Product>()
            .HasQueryFilter(p => !_tenantContext.TenantId.HasValue || p.TenantId == _tenantContext.TenantId);
        builder.Entity<StockMovement>()
            .HasQueryFilter(s => !_tenantContext.TenantId.HasValue || s.TenantId == _tenantContext.TenantId);
        builder.Entity<Activity>()
            .HasQueryFilter(a => !_tenantContext.TenantId.HasValue || a.TenantId == _tenantContext.TenantId);
        builder.Entity<WorkflowRule>()
            .HasQueryFilter(r => !_tenantContext.TenantId.HasValue || r.TenantId == _tenantContext.TenantId);
        builder.Entity<WorkflowLog>()
            .HasQueryFilter(l => !_tenantContext.TenantId.HasValue || l.TenantId == _tenantContext.TenantId);
        builder.Entity<TenantIntegration>()
            .HasQueryFilter(i => !_tenantContext.TenantId.HasValue || i.TenantId == _tenantContext.TenantId);
        builder.Entity<CustomRole>()
            .HasQueryFilter(r => !_tenantContext.TenantId.HasValue || r.TenantId == _tenantContext.TenantId);
        builder.Entity<RolePermission>()
            .HasQueryFilter(p => !_tenantContext.TenantId.HasValue || p.TenantId == _tenantContext.TenantId);
        builder.Entity<AuditLog>()
            .HasQueryFilter(a => !_tenantContext.TenantId.HasValue || a.TenantId == _tenantContext.TenantId);
        builder.Entity<Account>()
            .HasQueryFilter(a => !_tenantContext.TenantId.HasValue || a.TenantId == _tenantContext.TenantId);
        builder.Entity<JournalEntry>()
            .HasQueryFilter(j => !_tenantContext.TenantId.HasValue || j.TenantId == _tenantContext.TenantId);
        builder.Entity<JournalEntryLine>()
            .HasQueryFilter(l => !_tenantContext.TenantId.HasValue || l.TenantId == _tenantContext.TenantId);
        builder.Entity<UserPreferences>()
            .HasQueryFilter(u => !_tenantContext.TenantId.HasValue || u.TenantId == _tenantContext.TenantId);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var tenantId = _tenantContext.TenantId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity baseEntity && entry.State == EntityState.Modified)
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }

            if (entry.Entity is ITenantEntity tenantEntity && entry.State == EntityState.Added)
            {
                if (tenantId.HasValue && tenantEntity.TenantId == Guid.Empty)
                {
                    tenantEntity.TenantId = tenantId.Value;
                }
            }
        }
    }
}
