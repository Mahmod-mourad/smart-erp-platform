using FluentAssertions;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Chatbot;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Tests;

public class ChatContextBuilderTests
{
    private static (ChatContextBuilder Builder, AppDbContext Db, Guid TenantId) BuildSut(out TestHarness harness)
    {
        harness = new TestHarness();
        var tenantId = Guid.NewGuid();
        harness.TenantContext.SetTenant(tenantId);
        var db = harness.Get<AppDbContext>();
        return (new ChatContextBuilder(db), db, tenantId);
    }

    [Fact]
    public async Task Context_reflects_the_current_tenant_business_data()
    {
        var (builder, db, tenantId) = BuildSut(out _);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var customer = new Customer { TenantId = tenantId, Name = "Acme", Status = CustomerStatus.Active };
        db.Customers.Add(customer);
        db.Customers.Add(new Customer { TenantId = tenantId, Name = "Old", Status = CustomerStatus.Churned });

        db.Leads.Add(new Lead
        {
            TenantId = tenantId, Title = "Big deal", Value = 450_000m,
            Stage = LeadStage.Won, CustomerId = customer.Id, UpdatedAt = DateTime.UtcNow
        });

        var emp = new Employee
        {
            TenantId = tenantId, FirstName = "Sara", LastName = "Ali",
            Department = "Sales", Position = "Rep", Status = EmployeeStatus.Active
        };
        db.Employees.Add(emp);
        db.AttendanceRecords.Add(new AttendanceRecord
        {
            TenantId = tenantId, EmployeeId = emp.Id, Date = today, Status = AttendanceStatus.Present
        });

        db.Products.Add(new Product { TenantId = tenantId, Name = "Widget", IsLowStock = true });
        await db.SaveChangesAsync();

        var context = await builder.BuildContextAsync();

        context.Should().Contain("Active customers: 1");
        context.Should().Contain("Total customers: 2");
        context.Should().Contain("Won deals: 1");
        context.Should().Contain("450,000 EGP");
        context.Should().Contain("Active employees: 1");
        context.Should().Contain("Present today: 1/1");
        context.Should().Contain("Low-stock alerts: 1");
    }
}
