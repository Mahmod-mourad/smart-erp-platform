using System.ComponentModel.DataAnnotations.Schema;
using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>A single day's attendance for an employee (check-in/out or a leave/absence marker).</summary>
public class AttendanceRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly Date { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }

    public AttendanceStatus Status { get; set; }

    /// <summary>Worked duration, when both check-in and check-out are present. Not persisted.</summary>
    [NotMapped]
    public TimeSpan? WorkingHours =>
        CheckIn.HasValue && CheckOut.HasValue ? CheckOut.Value - CheckIn.Value : null;
}
