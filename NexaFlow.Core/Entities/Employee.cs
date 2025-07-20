using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>
/// An HR employee record owned by a tenant. Independent of the Identity login user, but may
/// optionally be linked to one via <see cref="UserId"/> (not every employee needs an account).
/// </summary>
public class Employee : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? NationalId { get; set; }

    public required string Department { get; set; }
    public required string Position { get; set; }
    public DateOnly HireDate { get; set; }

    public decimal BaseSalary { get; set; }
    public decimal Allowances { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>Optional link to the tenant's Identity user (an ApplicationUser id).</summary>
    public Guid? UserId { get; set; }
    
    /// <summary>Optional link to the branch this employee belongs to.</summary>
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
