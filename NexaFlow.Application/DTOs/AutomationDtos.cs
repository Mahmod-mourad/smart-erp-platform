using NexaFlow.Core.Enums;

namespace NexaFlow.Application.DTOs;

// TriggerType is exposed as its string name ("StockLow" | "ScheduledDaily" | ...) for the
// Angular client, matching the CRM/Inventory convention of stringifying enums in DTOs.

public record WorkflowRuleDto(
    Guid Id,
    string Name,
    string? Description,
    string TriggerType,
    string TriggerConfig,   // raw JSON, parsed/edited by the no-code builder
    string ActionsConfig,   // raw JSON array
    bool IsActive,
    DateTime? LastExecutedAt,
    int TotalExecutions,
    int SuccessfulExecutions,
    DateTime CreatedAt);

// TriggerType is accepted as its string name (e.g. "StockLow"), matching the CRM/Inventory
// convention of stringified enums in request DTOs; the service parses it.
public record CreateWorkflowRuleDto(
    string Name,
    string? Description,
    string TriggerType,
    string TriggerConfig,   // JSON string produced by the Angular builder
    string ActionsConfig);  // JSON array string

public record UpdateWorkflowRuleDto(
    string Name,
    string? Description,
    string TriggerType,
    string TriggerConfig,
    string ActionsConfig);

public record WorkflowLogDto(
    Guid Id,
    string RuleName,
    DateTime ExecutedAt,
    string Status,
    string Details,
    string? TriggerData);

public record WorkflowLogPageDto(
    IReadOnlyList<WorkflowLogDto> Items,
    int Total,
    int Page,
    int PageSize);

// ---- Strongly-typed trigger configs (deserialized from WorkflowRule.TriggerConfig) ----

/// <summary>StockLow: fires when stock is at/below <paramref name="Threshold"/>.</summary>
/// <param name="ProductId">A specific product, or null for every low-stock product.</param>
/// <param name="Threshold">Stock level to alert at; when 0, each product's own MinimumStock is used.</param>
public record StockLowTriggerConfig(Guid? ProductId, int Threshold);

/// <summary>EmployeeAbsent: fires for employees with no attendance by the deadline hour (Cairo time).</summary>
/// <param name="CheckDeadlineHour">Hour of day (0-23) after which a missing check-in counts as absent.</param>
/// <param name="EmployeeId">A specific employee, or null for all active employees.</param>
public record EmployeeAbsentTriggerConfig(int CheckDeadlineHour, Guid? EmployeeId);

/// <summary>ScheduledDaily: fires once per day at the configured local (Cairo) time.</summary>
public record ScheduledDailyTriggerConfig(int Hour, int Minute);

/// <summary>ScheduledWeekly: fires once per week on <paramref name="DayOfWeek"/> at the configured time.</summary>
public record ScheduledWeeklyTriggerConfig(DayOfWeek DayOfWeek, int Hour, int Minute);

// ---- Strongly-typed action configs (deserialized per element of WorkflowRule.ActionsConfig) ----
// Message/body fields support {{summary}} and {{timestamp}} placeholders.

public record SendEmailAction(string To, string Subject, string Body);
public record SendWhatsAppAction(string To, string Message);
public record SendSlackAction(string Message, string? Channel);
public record CreateActivityAction(Guid CustomerId, ActivityType ActivityType, string Subject);

/// <summary>AppendToSheet: appends one row; the template is split on '|' into columns.</summary>
public record AppendSheetAction(string Row);

/// <summary>Real-time payload pushed to the tenant's SignalR group when a rule fires.</summary>
public record WorkflowExecutedNotification(
    string RuleName,
    string Status,
    string Summary,
    DateTime ExecutedAt);
