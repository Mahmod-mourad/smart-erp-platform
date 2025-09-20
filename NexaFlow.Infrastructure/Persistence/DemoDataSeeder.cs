using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Identity;

namespace NexaFlow.Infrastructure.Persistence;

/// <summary>
/// Seeds a self-contained "Demo Company" tenant so a fresh deployment has something to show:
/// a login, customers/leads across the pipeline, employees, products with stock movements, and
/// an automation rule. Deterministic and idempotent — it no-ops once the demo account exists.
/// </summary>
public static class DemoDataSeeder
{
    public const string DemoEmail = "demo@nexaflow.com";
    public const string DemoPassword = "Demo@2025";
    public const string DemoCompany = "Demo Company";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetRequiredService<ILogger<DemoSeederLog>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Idempotent: if the demo admin already exists, the tenant is already seeded.
        if (await userManager.FindByEmailAsync(DemoEmail) is not null)
        {
            logger.LogInformation("Demo data already present; skipping.");
            return;
        }

        // Reuse the real onboarding path so the admin gets a proper password hash + role.
        var auth = services.GetRequiredService<IAuthService>();
        var registration = await auth.RegisterCompanyAsync(
            new RegisterCompanyRequest(DemoCompany, "Demo", "Admin", DemoEmail, DemoPassword), null, ct);

        var tenantId = registration.User.TenantId;
        var adminId = registration.User.Id;

        // Scope the DbContext to the demo tenant for the remaining writes/reads.
        services.GetRequiredService<ITenantContext>().SetTenant(tenantId);
        var db = services.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var rng = new Random(42); // deterministic variety

        var customers = SeedCustomers(db, tenantId, now, rng);
        SeedLeads(db, tenantId, now, rng, customers);
        SeedEmployees(db, tenantId, now);
        var products = SeedProducts(db, tenantId, now);
        SeedStockMovements(db, tenantId, now, rng, adminId, products);
        SeedAutomationRule(db, tenantId, products[0]);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded demo tenant {TenantId} ({Customers} customers, {Products} products).",
            tenantId, customers.Count, products.Count);
    }

    private static List<Customer> SeedCustomers(AppDbContext db, Guid tenantId, DateTime now, Random rng)
    {
        string[] names =
        [
            "Nile Trading Co.", "Cairo Tech Solutions", "Alexandria Logistics", "Delta Foods",
            "Red Sea Resorts", "Giza Construction", "Sphinx Media", "Pharaoh Pharma",
            "Sahara Energy", "Luxor Textiles", "Aswan Agriculture", "Sinai Mining",
            "Mediterranean Imports", "Pyramid Software", "Oasis Retail", "Falcon Logistics",
            "Cleopatra Cosmetics", "Horizon Telecom", "Nubia Finance", "Cataract Tours",
        ];

        var customers = new List<Customer>();
        for (var i = 0; i < names.Length; i++)
        {
            // ~1 in 7 churned, the rest active — gives the churn model both classes.
            var status = i % 7 == 3 ? CustomerStatus.Churned : CustomerStatus.Active;
            var customer = new Customer
            {
                TenantId = tenantId,
                Name = names[i],
                Email = $"contact@{names[i].Split(' ')[0].ToLowerInvariant()}.example",
                Phone = $"+2010{rng.Next(10000000, 99999999)}",
                Company = names[i],
                Status = status,
                CreatedAt = now.AddDays(-rng.Next(60, 400)),
            };
            customers.Add(customer);
        }

        db.Customers.AddRange(customers);
        return customers;
    }

    private static void SeedLeads(
        AppDbContext db, Guid tenantId, DateTime now, Random rng, List<Customer> customers)
    {
        var leads = new List<Lead>();

        // Won deals spread across the last 10 months (rising trend) to drive the sales forecast.
        for (var month = 10; month >= 1; month--)
        {
            var wonAt = now.AddMonths(-month);
            var dealsThisMonth = 2 + rng.Next(0, 2);
            for (var d = 0; d < dealsThisMonth; d++)
            {
                var customer = customers[rng.Next(customers.Count)];
                var value = 8000 + (10 - month) * 1500 + rng.Next(-1500, 2500);
                leads.Add(new Lead
                {
                    TenantId = tenantId,
                    Title = $"{customer.Name} — {wonAt:MMM yyyy} order",
                    Value = value,
                    Stage = LeadStage.Won,
                    CustomerId = customer.Id,
                    CreatedAt = wonAt.AddDays(-rng.Next(10, 40)),
                    UpdatedAt = wonAt,
                });
            }
        }

        // A handful of open opportunities across the pipeline for the Kanban board + KPIs.
        LeadStage[] openStages =
            [LeadStage.Prospect, LeadStage.Qualified, LeadStage.Proposal, LeadStage.Negotiation];
        for (var i = 0; i < 12; i++)
        {
            var customer = customers[rng.Next(customers.Count)];
            leads.Add(new Lead
            {
                TenantId = tenantId,
                Title = $"{customer.Name} — new opportunity",
                Value = 5000 + rng.Next(0, 30000),
                Stage = openStages[i % openStages.Length],
                CustomerId = customer.Id,
                CreatedAt = now.AddDays(-rng.Next(1, 45)),
                ExpectedCloseDate = now.AddDays(rng.Next(10, 60)),
            });
        }

        db.Leads.AddRange(leads);
    }

    private static void SeedEmployees(AppDbContext db, Guid tenantId, DateTime now)
    {
        (string First, string Last, string Dept, string Position, decimal Salary)[] people =
        [
            ("Mona", "Hassan", "Sales", "Sales Manager", 28000),
            ("Omar", "Khaled", "Engineering", "Senior Developer", 35000),
            ("Sara", "Adel", "HR", "HR Specialist", 18000),
            ("Youssef", "Tarek", "Operations", "Operations Lead", 26000),
            ("Laila", "Mostafa", "Finance", "Accountant", 22000),
        ];

        var today = DateOnly.FromDateTime(now);
        var employees = people.Select((p, i) => new Employee
        {
            TenantId = tenantId,
            FirstName = p.First,
            LastName = p.Last,
            Email = $"{p.First.ToLowerInvariant()}.{p.Last.ToLowerInvariant()}@nexaflow.com",
            Department = p.Dept,
            Position = p.Position,
            HireDate = today.AddDays(-(180 + i * 90)),
            BaseSalary = p.Salary,
            Allowances = p.Salary * 0.1m,
            Status = EmployeeStatus.Active,
        });

        db.Employees.AddRange(employees);
    }

    private static List<Product> SeedProducts(AppDbContext db, Guid tenantId, DateTime now)
    {
        (string Name, string Sku, string Category, decimal Price, int Stock, int Min)[] catalog =
        [
            ("Wireless Mouse", "ACC-001", "Accessories", 350, 120, 30),
            ("Mechanical Keyboard", "ACC-002", "Accessories", 1200, 18, 20),   // low
            ("27\" Monitor", "DSP-001", "Displays", 5500, 40, 10),
            ("USB-C Hub", "ACC-003", "Accessories", 800, 8, 15),                // low
            ("Laptop Stand", "ACC-004", "Accessories", 600, 75, 20),
            ("Webcam HD", "PER-001", "Peripherals", 1500, 22, 10),
            ("Noise-cancelling Headset", "PER-002", "Peripherals", 2800, 5, 12), // low
            ("Docking Station", "DSP-002", "Displays", 3200, 30, 8),
            ("Ergonomic Chair", "FUR-001", "Furniture", 4500, 14, 6),
            ("Standing Desk", "FUR-002", "Furniture", 9000, 9, 5),
        ];

        var products = catalog.Select(p => new Product
        {
            TenantId = tenantId,
            Name = p.Name,
            SKU = p.Sku,
            Category = p.Category,
            UnitPrice = p.Price,
            CurrentStock = p.Stock,
            MinimumStock = p.Min,
            IsLowStock = p.Stock < p.Min,
            CreatedAt = now.AddDays(-200),
        }).ToList();

        db.Products.AddRange(products);
        return products;
    }

    private static void SeedStockMovements(
        AppDbContext db, Guid tenantId, DateTime now, Random rng, Guid adminId, List<Product> products)
    {
        var movements = new List<StockMovement>();
        foreach (var product in products)
        {
            // Initial stock-in ~6 months ago.
            movements.Add(new StockMovement
            {
                TenantId = tenantId,
                ProductId = product.Id,
                Type = StockMovementType.In,
                Quantity = product.CurrentStock + rng.Next(20, 80),
                Reason = "Initial stock",
                CreatedById = adminId,
                CreatedAt = now.AddMonths(-6),
            });

            // Outbound consumption over the last 30 days to drive depletion predictions.
            var outCount = rng.Next(6, 12);
            for (var i = 0; i < outCount; i++)
            {
                movements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    Type = StockMovementType.Out,
                    Quantity = rng.Next(1, 6),
                    Reason = "Sales order",
                    CreatedById = adminId,
                    CreatedAt = now.AddDays(-rng.Next(0, 30)),
                });
            }
        }

        db.StockMovements.AddRange(movements);
    }

    private static void SeedAutomationRule(AppDbContext db, Guid tenantId, Product product)
    {
        db.WorkflowRules.Add(new WorkflowRule
        {
            TenantId = tenantId,
            Name = "Low stock email alert",
            Description = "Email the team when a product drops to its minimum stock level.",
            TriggerType = TriggerType.StockLow,
            TriggerConfig = $"{{\"productId\":\"{product.Id}\",\"threshold\":{product.MinimumStock}}}",
            ActionsConfig =
                "[{\"type\":\"SendEmail\",\"to\":\"" + DemoEmail +
                "\",\"subject\":\"Low stock alert\",\"body\":\"A product has reached its minimum stock level.\"}]",
            IsActive = true,
        });
    }

    /// <summary>Marker type for a typed logger category.</summary>
    public sealed class DemoSeederLog;
}
