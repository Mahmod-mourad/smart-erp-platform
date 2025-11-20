using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.ML;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Tests;

public class PredictionTests
{
    private static PredictionService NewService(AppDbContext db, TestCurrentUser currentUser) =>
        new(db, currentUser, new MemoryCache(new MemoryCacheOptions()), NullLogger<PredictionService>.Instance);

    [Fact]
    public async Task StockDepletion_computes_runway_and_orders_most_urgent_first()
    {
        var h = new TestHarness();
        var tenant = Guid.NewGuid();
        h.TenantContext.SetTenant(tenant);
        h.CurrentUser.TenantId = tenant;

        using var scope = h.Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // "Fast": 25 units above minimum, ~2/day  -> ~12 days to minimum.
        var fast = AddProduct(db, tenant, "Fast", currentStock: 30, minimumStock: 5);
        AddOutMovements(db, tenant, fast, totalQuantity: 60, overDays: 30);

        // "Slow": 90 units above minimum, ~1/day  -> ~90 days to minimum.
        var slow = AddProduct(db, tenant, "Slow", currentStock: 100, minimumStock: 10);
        AddOutMovements(db, tenant, slow, totalQuantity: 30, overDays: 30);
        await db.SaveChangesAsync();

        var results = await NewService(db, h.CurrentUser).GetStockDepletionAsync();

        results.Should().HaveCount(2);
        results[0].ProductName.Should().Be("Fast");           // most urgent first
        results[0].DailyConsumptionRate.Should().BeApproximately(2, 0.05);
        results[0].DaysUntilMinimum.Should().Be(12);
        results[0].DaysUntilDepletion.Should().Be(15);
        results[1].ProductName.Should().Be("Slow");
    }

    [Fact]
    public async Task StockDepletion_flags_low_confidence_when_no_outbound_movements()
    {
        var h = new TestHarness();
        var tenant = Guid.NewGuid();
        h.TenantContext.SetTenant(tenant);
        h.CurrentUser.TenantId = tenant;

        using var scope = h.Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        AddProduct(db, tenant, "Untouched", currentStock: 50, minimumStock: 10);
        await db.SaveChangesAsync();

        var result = (await NewService(db, h.CurrentUser).GetStockDepletionAsync()).Single();

        result.DailyConsumptionRate.Should().Be(0);
        result.DaysUntilDepletion.Should().BeNull();
        result.DaysUntilMinimum.Should().BeNull();
        result.PredictionConfidence.Should().Be("Low");
    }

    [Fact]
    public async Task SalesForecast_returns_insufficient_below_four_months()
    {
        var h = new TestHarness();
        var tenant = Guid.NewGuid();
        h.TenantContext.SetTenant(tenant);
        h.CurrentUser.TenantId = tenant;

        using var scope = h.Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = AddCustomer(db, tenant, "Acme", CustomerStatus.Active);
        AddWonLead(db, tenant, customer, value: 1000, wonAt: DateTime.UtcNow.AddMonths(-2));
        AddWonLead(db, tenant, customer, value: 1200, wonAt: DateTime.UtcNow.AddMonths(-1));
        await db.SaveChangesAsync();

        var result = await NewService(db, h.CurrentUser).ForecastSalesAsync();

        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Predictions.Should().BeEmpty();
    }

    [Fact]
    public async Task SalesForecast_produces_horizon_many_non_negative_predictions()
    {
        var h = new TestHarness();
        var tenant = Guid.NewGuid();
        h.TenantContext.SetTenant(tenant);
        h.CurrentUser.TenantId = tenant;

        using var scope = h.Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = AddCustomer(db, tenant, "Acme", CustomerStatus.Active);
        for (var i = 8; i >= 1; i--)
            AddWonLead(db, tenant, customer, value: 1000 + i * 100, wonAt: DateTime.UtcNow.AddMonths(-i));
        await db.SaveChangesAsync();

        var result = await NewService(db, h.CurrentUser).ForecastSalesAsync(monthsAhead: 3);

        result.IsSuccessful.Should().BeTrue();
        result.HistoricalData.Should().HaveCount(8);
        result.Predictions.Should().HaveCount(3);
        result.Predictions.Should().OnlyContain(p => p.PredictedValue >= 0);
    }

    [Fact]
    public async Task ChurnRisk_returns_empty_with_too_few_customers()
    {
        var h = new TestHarness();
        var tenant = Guid.NewGuid();
        h.TenantContext.SetTenant(tenant);
        h.CurrentUser.TenantId = tenant;

        using var scope = h.Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        for (var i = 0; i < 5; i++)
            AddCustomer(db, tenant, $"C{i}", CustomerStatus.Active);
        await db.SaveChangesAsync();

        var results = await NewService(db, h.CurrentUser).GetChurnRiskAsync();

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ChurnRisk_scores_every_customer_and_orders_by_probability_descending()
    {
        var h = new TestHarness();
        var tenant = Guid.NewGuid();
        h.TenantContext.SetTenant(tenant);
        h.CurrentUser.TenantId = tenant;

        using var scope = h.Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 12 customers, both classes present, with varied purchase recency/volume.
        for (var i = 0; i < 12; i++)
        {
            var churned = i % 3 == 0;
            var status = churned ? CustomerStatus.Churned : CustomerStatus.Active;
            var customer = AddCustomer(db, tenant, $"C{i}", status);
            if (!churned)
                AddWonLead(db, tenant, customer, value: 500 + i * 50, wonAt: DateTime.UtcNow.AddDays(-i * 3));
        }
        await db.SaveChangesAsync();

        var results = await NewService(db, h.CurrentUser).GetChurnRiskAsync(top: 10);

        results.Should().HaveCount(10);                       // capped at top
        results.Should().BeInDescendingOrder(r => r.ChurnProbability);
        results.Should().OnlyContain(r => r.ChurnProbability >= 0 && r.ChurnProbability <= 100);
        results.Should().OnlyContain(r => r.RiskLevel == "High" || r.RiskLevel == "Medium" || r.RiskLevel == "Low");
    }

    [Fact]
    public async Task Predictions_are_isolated_per_tenant()
    {
        var h = new TestHarness();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed products for tenant A only.
        h.TenantContext.SetTenant(tenantA);
        using (var scope = h.Provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AddProduct(db, tenantA, "A-only", currentStock: 10, minimumStock: 1);
            await db.SaveChangesAsync();
        }

        // Acting as tenant B, the tenant-filtered query exposes nothing from tenant A.
        h.TenantContext.SetTenant(tenantB);
        h.CurrentUser.TenantId = tenantB;
        using (var scope = h.Provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var results = await NewService(db, h.CurrentUser).GetStockDepletionAsync();
            results.Should().BeEmpty();
        }
    }

    // ── seeding helpers ──────────────────────────────────────────────────────
    private static Product AddProduct(AppDbContext db, Guid tenant, string name, int currentStock, int minimumStock)
    {
        var product = new Product
        {
            TenantId = tenant,
            Name = name,
            CurrentStock = currentStock,
            MinimumStock = minimumStock,
            IsLowStock = currentStock < minimumStock
        };
        db.Products.Add(product);
        return product;
    }

    private static void AddOutMovements(AppDbContext db, Guid tenant, Product product, int totalQuantity, int overDays)
    {
        // Oldest movement carries the full span so the daily rate works out to totalQuantity/overDays.
        db.StockMovements.Add(new StockMovement
        {
            TenantId = tenant,
            ProductId = product.Id,
            Type = StockMovementType.Out,
            Quantity = totalQuantity,
            Reason = "test",
            CreatedAt = DateTime.UtcNow.AddDays(-overDays)
        });
    }

    private static Customer AddCustomer(AppDbContext db, Guid tenant, string name, CustomerStatus status)
    {
        var customer = new Customer { TenantId = tenant, Name = name, Status = status };
        db.Customers.Add(customer);
        return customer;
    }

    private static void AddWonLead(AppDbContext db, Guid tenant, Customer customer, decimal value, DateTime wonAt)
    {
        db.Leads.Add(new Lead
        {
            TenantId = tenant,
            Title = "Deal",
            Value = value,
            Stage = LeadStage.Won,
            CustomerId = customer.Id,
            CreatedAt = wonAt,
            UpdatedAt = wonAt
        });
    }
}
