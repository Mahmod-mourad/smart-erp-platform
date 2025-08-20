namespace NexaFlow.Application.DTOs;

// Movement Type is exposed as its string name ('In' | 'Out') for the Angular client,
// matching the CRM convention (see CrmDtos.cs).

public record ProductDto(
    Guid Id,
    string Name,
    string? SKU,
    string? Category,
    decimal UnitPrice,
    int CurrentStock,
    int MinimumStock,
    bool IsLowStock,
    DateTime CreatedAt);

public record CreateProductDto(
    string Name,
    string? SKU,
    string? Category,
    decimal UnitPrice,
    int MinimumStock,
    string? Description);

public record UpdateProductDto(
    string Name,
    decimal UnitPrice,
    int MinimumStock,
    string? Description);

public record AddStockMovementDto(
    string Type,
    int Quantity,
    string Reason);

public record StockMovementDto(
    Guid Id,
    string Type,
    int Quantity,
    string Reason,
    DateTime CreatedAt,
    string? CreatedByName);
