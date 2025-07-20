using System.ComponentModel.DataAnnotations.Schema;
using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>An employee's request for leave, reviewed (approved/rejected) by a manager.</summary>
public class LeaveRequest : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public LeaveType Type { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    public string? ReviewNote { get; set; }

    /// <summary>The Identity user (ApplicationUser id) who reviewed the request.</summary>
    public Guid? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Inclusive day span of the request. Not persisted.</summary>
    [NotMapped]
    public int TotalDays => (EndDate.DayNumber - StartDate.DayNumber) + 1;
}
