using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.ML;

/// <summary>
/// ML.NET predictions for the ambient tenant. All data access goes through the tenant-filtered
/// <see cref="AppDbContext"/>, so queries never leak across tenants; the trained churn model is
/// additionally cached per-tenant so one tenant can never score against another's model.
/// </summary>
public sealed class PredictionService(
    AppDbContext db,
    ICurrentUser currentUser,
    IMemoryCache cache,
    ILogger<PredictionService> logger) : IPredictionService
{
    private const int MinMonthsForForecast = 4;
    private const int MinCustomersForChurn = 10;
    private static readonly TimeSpan ChurnModelTtl = TimeSpan.FromMinutes(30);

    // Serialises churn training per tenant so concurrent requests don't train the same model twice.
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> TrainingGates = new();

    private Guid TenantId => currentUser.TenantId
        ?? throw new UnauthorizedAppException("No tenant in the current context.");

    // ── Sales forecast (SSA) ─────────────────────────────────────────────────
    public async Task<SalesForecastResult> ForecastSalesAsync(int monthsAhead = 3, CancellationToken ct = default)
    {
        monthsAhead = Math.Clamp(monthsAhead, 1, 12);

        var wonLeads = await db.Leads
            .Where(l => l.Stage == LeadStage.Won)
            .Select(l => new { Date = l.UpdatedAt ?? l.CreatedAt, l.Value })
            .ToListAsync(ct);

        var monthlySales = wonLeads
            .GroupBy(l => new { l.Date.Year, l.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new SalesDataPoint { Value = (float)g.Sum(x => x.Value) })
            .ToList();

        if (monthlySales.Count < MinMonthsForForecast)
        {
            logger.LogInformation(
                "Sales forecast skipped for tenant {TenantId}: only {Count} month(s) of data.",
                TenantId, monthlySales.Count);
            return SalesForecastResult.Insufficient();
        }

        var series = monthlySales.Select(d => d.Value).ToArray();

        // Prefer ML.NET SSA; fall back to a deterministic linear trend when the SSA native
        // libraries aren't available on the host (e.g. Intel MKL is absent on osx-arm64).
        var bands = TryForecastSsa(series, monthsAhead) ?? ForecastByTrend(series, monthsAhead);

        var firstMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1);
        var predictions = Enumerable.Range(0, monthsAhead)
            .Select(i => new MonthlyPrediction(
                Month: new DateOnly(firstMonth.AddMonths(i).Year, firstMonth.AddMonths(i).Month, 1),
                PredictedValue: NonNegative(bands.Values[i]),
                LowerBound: NonNegative(bands.Lower[i]),
                UpperBound: NonNegative(bands.Upper[i])))
            .ToList();

        return new SalesForecastResult(
            IsSuccessful: true,
            ErrorMessage: null,
            HistoricalData: series.Select(v => (decimal)v).ToList(),
            Predictions: predictions,
            GeneratedAt: DateTime.UtcNow);
    }

    private ForecastBands? TryForecastSsa(float[] series, int horizon)
    {
        try
        {
            var mlContext = new MLContext(seed: 42);
            var dataView = mlContext.Data.LoadFromEnumerable(series.Select(v => new SalesDataPoint { Value = v }));

            // SSA requires trainSize > 2 * windowSize, so keep the window strictly below half.
            var windowSize = Math.Max(2, Math.Min(4, (series.Length - 1) / 2));
            var pipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(SalesForecastOutput.ForecastedValues),
                inputColumnName: nameof(SalesDataPoint.Value),
                windowSize: windowSize,
                seriesLength: series.Length,
                trainSize: series.Length,
                horizon: horizon,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: nameof(SalesForecastOutput.ConfidenceLowerBound),
                confidenceUpperBoundColumn: nameof(SalesForecastOutput.ConfidenceUpperBound));

            var model = pipeline.Fit(dataView);
            var engine = model.CreateTimeSeriesEngine<SalesDataPoint, SalesForecastOutput>(mlContext);
            var forecast = engine.Predict();

            return new ForecastBands(forecast.ForecastedValues, forecast.ConfidenceLowerBound, forecast.ConfidenceUpperBound);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SSA forecast unavailable for tenant {TenantId}; using trend fallback.", TenantId);
            return null;
        }
    }

    /// <summary>Deterministic least-squares linear trend with a residual-based 95% band.</summary>
    private static ForecastBands ForecastByTrend(float[] series, int horizon)
    {
        var n = series.Length;
        double meanX = (n - 1) / 2.0;
        double meanY = series.Average(v => (double)v);
        double sxx = 0, sxy = 0;
        for (var i = 0; i < n; i++)
        {
            sxx += (i - meanX) * (i - meanX);
            sxy += (i - meanX) * (series[i] - meanY);
        }

        var slope = sxx > 0 ? sxy / sxx : 0;
        var intercept = meanY - slope * meanX;

        double ssr = 0;
        for (var i = 0; i < n; i++)
        {
            var fit = intercept + slope * i;
            ssr += (series[i] - fit) * (series[i] - fit);
        }
        var stdErr = n > 2 ? Math.Sqrt(ssr / (n - 2)) : 0;
        var margin = 1.96 * stdErr;

        var values = new float[horizon];
        var lower = new float[horizon];
        var upper = new float[horizon];
        for (var i = 0; i < horizon; i++)
        {
            var point = intercept + slope * (n + i);
            values[i] = (float)point;
            lower[i] = (float)(point - margin);
            upper[i] = (float)(point + margin);
        }

        return new ForecastBands(values, lower, upper);
    }

    private sealed record ForecastBands(float[] Values, float[] Lower, float[] Upper);

    // ── Churn risk (FastTree) ────────────────────────────────────────────────
    public async Task<IReadOnlyList<CustomerChurnResult>> GetChurnRiskAsync(int top = 10, CancellationToken ct = default)
    {
        top = Math.Clamp(top, 1, 100);

        var customers = await db.Customers.ToListAsync(ct);
        if (customers.Count == 0) return [];

        var leadsByCustomer = (await db.Leads.ToListAsync(ct))
            .GroupBy(l => l.CustomerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var model = await GetOrTrainChurnModelAsync(customers, leadsByCustomer, ct);
        if (model is null) return [];

        var engine = model.Context.Model.CreatePredictionEngine<CustomerFeatures, ChurnPrediction>(model.Transformer);

        var results = new List<CustomerChurnResult>(customers.Count);
        foreach (var customer in customers)
        {
            var leads = leadsByCustomer.GetValueOrDefault(customer.Id) ?? [];
            var prediction = engine.Predict(BuildFeatures(customer, leads));
            var probability = Math.Clamp((int)Math.Round(prediction.Probability * 100), 0, 100);

            results.Add(new CustomerChurnResult(
                CustomerId: customer.Id,
                CustomerName: customer.Name,
                ChurnProbability: probability,
                RiskLevel: prediction.Probability switch
                {
                    >= 0.7f => "High",
                    >= 0.4f => "Medium",
                    _ => "Low"
                }));
        }

        return results
            .OrderByDescending(r => r.ChurnProbability)
            .Take(top)
            .ToList();
    }

    // ── Stock depletion (deterministic math) ─────────────────────────────────
    public async Task<IReadOnlyList<StockDepletionResult>> GetStockDepletionAsync(CancellationToken ct = default)
    {
        var products = await db.Products.ToListAsync(ct);
        if (products.Count == 0) return [];

        var movementsByProduct = (await db.StockMovements
                .Where(m => m.Type == StockMovementType.Out)
                .ToListAsync(ct))
            .GroupBy(m => m.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.CreatedAt).Take(30).ToList());

        var now = DateTime.UtcNow;
        var results = new List<StockDepletionResult>(products.Count);

        foreach (var product in products)
        {
            var outMovements = movementsByProduct.GetValueOrDefault(product.Id) ?? [];
            if (outMovements.Count == 0)
            {
                results.Add(new StockDepletionResult(
                    product.Id, product.Name, product.CurrentStock, product.MinimumStock,
                    DailyConsumptionRate: 0, DaysUntilDepletion: null, DaysUntilMinimum: null,
                    PredictionConfidence: "Low"));
                continue;
            }

            var daysCovered = Math.Max(1, (now - outMovements.Min(m => m.CreatedAt)).TotalDays);
            var totalConsumed = outMovements.Sum(m => m.Quantity);
            var dailyRate = totalConsumed / daysCovered;

            int? daysUntilDepletion = dailyRate > 0
                ? (int)Math.Floor(product.CurrentStock / dailyRate)
                : null;

            var stockAboveMin = product.CurrentStock - product.MinimumStock;
            int? daysUntilMinimum = dailyRate > 0 && stockAboveMin > 0
                ? (int)Math.Floor(stockAboveMin / dailyRate)
                : dailyRate > 0 ? 0 : null;

            results.Add(new StockDepletionResult(
                product.Id, product.Name, product.CurrentStock, product.MinimumStock,
                DailyConsumptionRate: Math.Round(dailyRate, 2),
                DaysUntilDepletion: daysUntilDepletion,
                DaysUntilMinimum: daysUntilMinimum,
                PredictionConfidence: outMovements.Count >= 10 ? "High"
                    : outMovements.Count >= 5 ? "Medium" : "Low"));
        }

        return results.OrderBy(r => r.DaysUntilMinimum ?? int.MaxValue).ToList();
    }

    // ── Dashboard summary (one round-trip; runs sequentially — DbContext is single-threaded) ──
    public async Task<PredictionDashboardSummary> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        var salesForecast = await ForecastSalesAsync(3, ct);
        var stockDepletion = await GetStockDepletionAsync(ct);
        var churnRisk = await GetChurnRiskAsync(10, ct);

        var activeCustomers = await db.Customers.CountAsync(c => c.Status == CustomerStatus.Active, ct);
        var openLeads = await db.Leads.CountAsync(
            l => l.Stage != LeadStage.Won && l.Stage != LeadStage.Lost, ct);
        var employees = await db.Employees.CountAsync(e => e.Status == EmployeeStatus.Active, ct);
        var lowStockItems = await db.Products.CountAsync(p => p.IsLowStock, ct);

        var stockAlerts = stockDepletion
            .Where(r => r.DaysUntilMinimum is < 14)
            .Take(5)
            .ToList();
        var highChurnRisk = churnRisk
            .Where(r => r.RiskLevel == "High")
            .ToList();

        return new PredictionDashboardSummary(
            SalesForecast: salesForecast,
            StockAlerts: stockAlerts,
            HighChurnRisk: highChurnRisk,
            ActiveCustomers: activeCustomers,
            OpenLeads: openLeads,
            Employees: employees,
            LowStockItems: lowStockItems,
            GeneratedAt: DateTime.UtcNow);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private async Task<ChurnModel?> GetOrTrainChurnModelAsync(
        List<Customer> customers, Dictionary<Guid, List<Lead>> leadsByCustomer, CancellationToken ct)
    {
        var cacheKey = $"churn-model:{TenantId}";
        if (cache.TryGetValue<ChurnModel?>(cacheKey, out var cached))
            return cached;

        var gate = TrainingGates.GetOrAdd(TenantId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue<ChurnModel?>(cacheKey, out cached))
                return cached;

            var model = TrainChurnModel(customers, leadsByCustomer);
            cache.Set(cacheKey, model, new MemoryCacheEntryOptions { SlidingExpiration = ChurnModelTtl });
            return model;
        }
        finally
        {
            gate.Release();
        }
    }

    private ChurnModel? TrainChurnModel(List<Customer> customers, Dictionary<Guid, List<Lead>> leadsByCustomer)
    {
        if (customers.Count < MinCustomersForChurn)
        {
            logger.LogInformation(
                "Churn model not trained for tenant {TenantId}: only {Count} customer(s).",
                TenantId, customers.Count);
            return null;
        }

        var trainingData = customers
            .Select(c => BuildFeatures(c, leadsByCustomer.GetValueOrDefault(c.Id) ?? [], includeLabel: true))
            .ToList();

        // FastTree needs both classes present; otherwise it cannot fit a meaningful boundary.
        if (trainingData.All(f => f.HasChurned) || trainingData.All(f => !f.HasChurned))
        {
            logger.LogInformation(
                "Churn model not trained for tenant {TenantId}: only one class present.", TenantId);
            return null;
        }

        try
        {
            var mlContext = new MLContext(seed: 42);
            var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = mlContext.Transforms.Concatenate(
                    "Features",
                    nameof(CustomerFeatures.DaysSinceLastPurchase),
                    nameof(CustomerFeatures.TotalPurchases),
                    nameof(CustomerFeatures.AveragePurchaseValue),
                    nameof(CustomerFeatures.TotalSpent),
                    nameof(CustomerFeatures.MonthsAsCustomer))
                .Append(mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    numberOfLeaves: 20,
                    numberOfTrees: 100,
                    minimumExampleCountPerLeaf: 5));

            var transformer = pipeline.Fit(dataView);
            return new ChurnModel(mlContext, transformer);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Churn model training failed for tenant {TenantId}.", TenantId);
            return null;
        }
    }

    private static CustomerFeatures BuildFeatures(Customer customer, List<Lead> leads, bool includeLabel = false)
    {
        var wonLeads = leads.Where(l => l.Stage == LeadStage.Won).ToList();
        var lastPurchase = wonLeads.Count > 0
            ? wonLeads.Max(l => l.UpdatedAt ?? l.CreatedAt)
            : customer.CreatedAt;

        return new CustomerFeatures
        {
            DaysSinceLastPurchase = (float)(DateTime.UtcNow - lastPurchase).TotalDays,
            TotalPurchases = wonLeads.Count,
            AveragePurchaseValue = wonLeads.Count > 0 ? (float)wonLeads.Average(l => l.Value) : 0,
            TotalSpent = (float)wonLeads.Sum(l => l.Value),
            MonthsAsCustomer = (float)((DateTime.UtcNow - customer.CreatedAt).TotalDays / 30),
            HasChurned = includeLabel && customer.Status == CustomerStatus.Churned
        };
    }

    private static decimal NonNegative(float value) =>
        value > 0 && float.IsFinite(value) ? (decimal)value : 0m;

    /// <summary>A trained churn model plus the context needed to create scoring engines from it.</summary>
    private sealed record ChurnModel(MLContext Context, ITransformer Transformer);
}
