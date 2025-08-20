namespace NexaFlow.Application.DTOs;

// Enums (Status / Type) are exposed as their string name so the Angular client can use
// string-literal unions directly, matching the CRM convention (see CrmDtos.cs). DateOnly
// serializes as 'yyyy-MM-dd'; attendance times are pre-formatted to 'HH:mm' in the service.

// ----- Employees -----

public record EmployeeDto(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Department,
    string Position,
    DateOnly HireDate,
    decimal BaseSalary,
    decimal Allowances,
    string Status,
    Guid? BranchId,
    DateTime CreatedAt);

public record CreateEmployeeDto(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? NationalId,
    string Department,
    string Position,
    DateOnly HireDate,
    decimal BaseSalary,
    decimal Allowances,
    Guid? BranchId);

public record UpdateEmployeeDto(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Department,
    string Position,
    decimal BaseSalary,
    decimal Allowances,
    string Status,
    Guid? BranchId);

// ----- Attendance -----

public record CheckInDto(Guid EmployeeId);

public record CheckOutDto(Guid AttendanceRecordId);

public record AttendanceDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    string? CheckIn,
    string? CheckOut,
    string Status,
    string? WorkingHours);

public record AttendanceSummaryDto(
    DateOnly Date,
    int Present,
    int Absent,
    int Late,
    int OnLeave);

// ----- Leave requests -----

public record CreateLeaveRequestDto(
    string Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);

public record ReviewLeaveDto(
    bool Approved,
    string? ReviewNote);

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string Status,
    int TotalDays,
    string? ReviewNote,
    string? ReviewedByName);

// ----- Payroll -----

public record PayslipDto(
    Guid EmployeeId,
    string EmployeeFullName,
    string Department,
    string Position,
    DateOnly Month,
    decimal BaseSalary,
    decimal Allowances,
    decimal GrossSalary,
    int WorkingDays,
    int PresentDays,
    int LeaveDays,
    int AbsentDays,
    decimal DailyRate,
    decimal AbsenceDeduction,
    decimal NetSalary);
