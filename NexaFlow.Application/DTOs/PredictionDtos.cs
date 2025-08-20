namespace NexaFlow.Application.DTOs;

// ML.NET prediction results (Sprint 7). These are pure DTOs — the ML.NET input/output
// types (with their column attributes) live in the Infrastructure ML layer, since the
// Application layer must not reference Microsoft.ML.

/// <summary>One forecasted month with its 95% confidence interval.</summary>
public record MonthlyPrediction(
    DateOnly Month,
    decimal PredictedValue,
    decimal LowerBound,
    decimal UpperBound);

/// <summary>Sales forecast output: historical series + future predictions, or a failure reason.</summary>
public record SalesForecastResult(
    bool IsSuccessful,
    string? ErrorMessage,
    IReadOnlyList<decimal> HistoricalData,
    IReadOnlyList<MonthlyPrediction> Predictions,
    DateTime GeneratedAt)
{
    public static SalesForecastResult Insufficient() => new(
        IsSuccessful: false,
        ErrorMessage: "Need at least 4 months of won-sales data to forecast.",
        HistoricalData: Array.Empty<decimal>(),
        Predictions: Array.Empty<MonthlyPrediction>(),
        GeneratedAt: DateTime.UtcNow);
}

/// <summary>A customer's churn risk: probability (0–100) and a bucketed risk level.</summary>
public record CustomerChurnResult(
    Guid CustomerId,
    string CustomerName,
    int ChurnProbability,   // 0–100
    string RiskLevel);      // High | Medium | Low

/// <summary>Projected stock runway for a product based on recent outbound movements.</summary>
public record StockDepletionResult(
    Guid ProductId,
    string ProductName,
    int CurrentStock,
    int MinimumStock,
    double DailyConsumptionRate,
    int? DaysUntilDepletion,
    int? DaysUntilMinimum,
    string PredictionConfidence);   // High | Medium | Low

/// <summary>Everything the dashboard needs in a single request.</summary>
public record PredictionDashboardSummary(
    SalesForecastResult SalesForecast,
    IReadOnlyList<StockDepletionResult> StockAlerts,
    IReadOnlyList<CustomerChurnResult> HighChurnRisk,
    int ActiveCustomers,
    int OpenLeads,
    int Employees,
    int LowStockItems,
    DateTime GeneratedAt);
