namespace NexaFlow.Core.Enums;

public enum TenantStatus
{
    PendingSetup = 0,
    Active = 1,
    Suspended = 2
}

public enum SubscriptionPlan
{
    Free = 0,
    Pro = 1,
    Enterprise = 2
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}

public enum CustomerStatus
{
    Active = 0,
    Inactive = 1,
    Lead = 2,
    Churned = 3
}

public enum LeadStage
{
    Prospect = 0,
    Qualified = 1,
    Proposal = 2,
    Negotiation = 3,
    Won = 4,
    Lost = 5
}

public enum EmployeeStatus
{
    Active = 0,
    OnLeave = 1,
    Terminated = 2
}

public enum AttendanceStatus
{
    Present = 0,
    Absent = 1,
    OnLeave = 2,
    Late = 3      // check-in after 9 AM
}

public enum LeaveType
{
    Annual = 0,
    Sick = 1,
    Emergency = 2,
    Unpaid = 3
}

public enum LeaveStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

public enum StockMovementType
{
    In = 0,
    Out = 1
}

public enum ActivityType
{
    Note = 0,
    Call = 1,
    Email = 2,
    Meeting = 3,
    StatusChange = 4   // system-generated when a lead moves or a customer is created
}

/// <summary>The condition that fires an automation WorkflowRule. Drives which ITriggerHandler runs.</summary>
public enum TriggerType
{
    StockLow = 0,            // a product's stock fell to/below its threshold
    EmployeeAbsent = 1,      // an active employee has no attendance by the check deadline
    LeaveRequestPending = 2, // a leave request is awaiting review
    ScheduledDaily = 3,      // fires once a day at a configured hour/minute
    ScheduledWeekly = 4      // fires once a week on a configured day at a configured time
}

/// <summary>Outcome of a single WorkflowRule execution, recorded on the WorkflowLog.</summary>
public enum WorkflowLogStatus
{
    Success = 0,        // every action succeeded
    PartialSuccess = 1, // some actions succeeded, some failed
    Failed = 2          // every action failed
}

/// <summary>A third-party integration a tenant can connect (credentials stored per tenant).</summary>
public enum IntegrationType
{
    WhatsApp = 0,     // Meta WhatsApp Business Cloud API
    Gmail = 1,        // Gmail SMTP (app password)
    Slack = 2,        // Slack incoming webhook
    GoogleSheets = 3  // Google Sheets export via a service account
}
