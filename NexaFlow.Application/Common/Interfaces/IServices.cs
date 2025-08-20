using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request, string? ip, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? ip, CancellationToken ct = default);
    Task<AuthResponse> AcceptInvitationAsync(AcceptInvitationRequest request, string? ip, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}

public interface ITenantService
{
    Task<TenantDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TenantDto> GetCurrentAsync(CancellationToken ct = default);
}

public interface IInvitationService
{
    Task<InvitationDto> InviteAsync(InviteMemberRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InvitationDto>> GetPendingAsync(CancellationToken ct = default);
    Task RevokeAsync(Guid invitationId, CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerDto request, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerDto request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ILeadService
{
    Task<IReadOnlyList<LeadDto>> GetAllAsync(CancellationToken ct = default);
    Task<LeadDto> CreateAsync(CreateLeadDto request, CancellationToken ct = default);
    Task<LeadDto> UpdateStageAsync(Guid id, UpdateLeadStageDto request, CancellationToken ct = default);
}

public interface IActivityService
{
    Task<IReadOnlyList<ActivityDto>> GetForCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<ActivityDto> CreateAsync(Guid customerId, CreateActivityDto request, CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default);
    Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, CancellationToken ct = default);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IAttendanceService
{
    Task<AttendanceDto> CheckInAsync(Guid employeeId, CancellationToken ct = default);
    Task<AttendanceDto> CheckOutAsync(Guid attendanceRecordId, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceDto>> GetEmployeeMonthlyAsync(Guid employeeId, int year, int month, CancellationToken ct = default);
    Task<AttendanceSummaryDto> GetDailySummaryAsync(DateOnly date, CancellationToken ct = default);
}

public interface ILeaveService
{
    Task<LeaveRequestDto> CreateAsync(CreateLeaveRequestDto request, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(string? status, CancellationToken ct = default);
    Task<LeaveRequestDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LeaveRequestDto> ReviewLeaveAsync(Guid id, ReviewLeaveDto request, CancellationToken ct = default);
}

public interface IPayrollService
{
    Task<PayslipDto> CalculatePayslipAsync(Guid employeeId, int year, int month, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto request, CancellationToken ct = default);
    Task<ProductDto> AddMovementAsync(Guid productId, AddStockMovementDto request, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetLowStockAsync(CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IAutomationService
{
    Task<IReadOnlyList<WorkflowRuleDto>> GetRulesAsync(CancellationToken ct = default);
    Task<WorkflowRuleDto> GetRuleAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowRuleDto> CreateAsync(CreateWorkflowRuleDto request, CancellationToken ct = default);
    Task<WorkflowRuleDto> UpdateAsync(Guid id, UpdateWorkflowRuleDto request, CancellationToken ct = default);
    Task<WorkflowRuleDto> ToggleAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowLogPageDto> GetLogsAsync(Guid ruleId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Run a single rule on demand (bypasses the scheduler) for the current tenant.</summary>
    Task TestRuleAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Engine entry point: evaluate every active rule for the ambient tenant (set via
    /// <see cref="ITenantContext"/>). Called per-tenant by the Hangfire automation job.
    /// </summary>
    Task EvaluateActiveRulesAsync(CancellationToken ct = default);
}

/// <summary>Pushes real-time workflow events to clients. Implemented over SignalR in the API layer.</summary>
public interface IWorkflowNotifier
{
    Task WorkflowExecutedAsync(Guid tenantId, WorkflowExecutedNotification payload, CancellationToken ct = default);
}

/// <summary>
/// Manages the current tenant's integration connections. Reads/writes <c>TenantIntegration</c> rows
/// and never exposes the stored credentials back to callers.
/// </summary>
public interface IIntegrationService
{
    Task<IReadOnlyList<IntegrationDto>> GetAllAsync(CancellationToken ct = default);
    Task<IntegrationDto> UpsertAsync(IntegrationType type, UpsertIntegrationDto request, CancellationToken ct = default);

    /// <summary>Sends a canned test message through the integration and records the outcome.</summary>
    Task<IntegrationTestResultDto> TestAsync(IntegrationType type, CancellationToken ct = default);
}

/// <summary>
/// Resolves the decrypted, strongly-typed config for an integration belonging to the ambient tenant
/// (see <see cref="ITenantContext"/>). Returns null when the integration is missing or disabled —
/// senders use this to degrade gracefully instead of throwing.
/// </summary>
public interface IIntegrationConfigProvider
{
    Task<T?> GetConfigAsync<T>(IntegrationType type, CancellationToken ct = default) where T : class;
}

/// <summary>
/// ML.NET-backed predictions for the current tenant (resolved via the DbContext tenant
/// filter and <see cref="ICurrentUser"/>). Models are trained on the tenant's own data and
/// never share state across tenants.
/// </summary>
public interface IPredictionService
{
    /// <summary>Forecast monthly won-sales for the next <paramref name="monthsAhead"/> months (SSA).</summary>
    Task<SalesForecastResult> ForecastSalesAsync(int monthsAhead = 3, CancellationToken ct = default);

    /// <summary>Top <paramref name="top"/> customers by churn probability (FastTree), highest first.</summary>
    Task<IReadOnlyList<CustomerChurnResult>> GetChurnRiskAsync(int top = 10, CancellationToken ct = default);

    /// <summary>Stock runway per product, most urgent first (pure consumption-rate math).</summary>
    Task<IReadOnlyList<StockDepletionResult>> GetStockDepletionAsync(CancellationToken ct = default);

    /// <summary>All predictions plus headline KPI counts, in one round-trip for the dashboard.</summary>
    Task<PredictionDashboardSummary> GetDashboardSummaryAsync(CancellationToken ct = default);
}

/// <summary>Answers business questions for the current tenant via a local Ollama model.</summary>
public interface IChatbotService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default);
}

/// <summary>Appends a row to the tenant's configured Google Sheet. No-op when not configured.</summary>
public interface IGoogleSheetsSender
{
    Task AppendRowAsync(IEnumerable<string> values, CancellationToken ct = default);
}

/// <summary>Sends WhatsApp messages. Dev no-op logs instead of sending — swap for Twilio/Meta in prod.</summary>
public interface IWhatsAppSender
{
    Task SendMessageAsync(string to, string message, CancellationToken ct = default);
}

/// <summary>Posts Slack messages. Dev no-op logs instead of sending — swap for a webhook client in prod.</summary>
public interface ISlackSender
{
    Task SendAsync(string message, string? channel, CancellationToken ct = default);
}

/// <summary>Issues JWT access tokens + opaque refresh tokens. Implemented in Infrastructure.</summary>
public interface IJwtTokenService
{
    (string token, DateTime expiresAtUtc) GenerateAccessToken(
        Guid userId, string email, string fullName, Guid tenantId, IEnumerable<string> roles, IEnumerable<string>? permissions = null);

    string GenerateRefreshToken();
}

/// <summary>Sends transactional emails (invites, etc.). No-op implementation in dev.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
