namespace NexaFlow.Application.DTOs;

// Status / Stage are exposed as strings (the enum name) so the Angular client can use
// string-literal unions ('Active' | 'Inactive' | ...) directly. Other DTOs in this app
// serialize enums as numbers; CRM intentionally differs to match the frontend contract.

public record CustomerDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Company,
    string Status,
    string? AssignedToName,
    int LeadsCount,
    DateTime CreatedAt);

public record CreateCustomerDto(
    string Name,
    string? Email,
    string? Phone,
    string? Company,
    string? Notes,
    Guid? AssignedToId);

public record UpdateCustomerDto(
    string Name,
    string? Email,
    string? Phone,
    string? Company,
    string? Notes,
    string Status,
    Guid? AssignedToId);

public record LeadDto(
    Guid Id,
    string Title,
    decimal Value,
    string Stage,
    Guid CustomerId,
    string CustomerName,
    string? AssignedToName,
    DateTime? ExpectedCloseDate);

public record CreateLeadDto(
    string Title,
    decimal Value,
    Guid CustomerId,
    Guid? AssignedToId,
    DateTime? ExpectedCloseDate);

public record UpdateLeadStageDto(string Stage);

// Type is exposed as the enum name ('Note' | 'Call' | 'Email' | 'Meeting' | 'StatusChange')
// for the same string-literal-union reason as Customer.Status / Lead.Stage above.
public record ActivityDto(
    Guid Id,
    Guid CustomerId,
    string Type,
    string Content,
    string? CreatedByName,
    DateTime CreatedAt);

public record CreateActivityDto(
    string Type,
    string Content);
