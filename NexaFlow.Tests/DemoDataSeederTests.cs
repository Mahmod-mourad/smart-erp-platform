using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Tests;

public class DemoDataSeederTests
{
    [Fact]
    public async Task Seeds_a_demo_tenant_with_a_full_dataset()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();

        using var scope = h.Provider.CreateScope();
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); // tenant set by the seeder
        (await db.Customers.CountAsync()).Should().Be(20);
        (await db.Products.CountAsync()).Should().Be(10);
        (await db.Employees.CountAsync()).Should().Be(5);
        (await db.Leads.CountAsync(l => l.Stage == LeadStage.Won)).Should().BeGreaterThan(0);
        (await db.WorkflowRules.CountAsync()).Should().Be(1);

        // The demo admin can be looked up and is wired to the seeded tenant.
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var login = await auth.LoginAsync(new(DemoDataSeeder.DemoEmail, DemoDataSeeder.DemoPassword), null);
        login.User.Email.Should().Be(DemoDataSeeder.DemoEmail);
    }

    [Fact]
    public async Task Is_idempotent_and_does_not_duplicate_on_second_run()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();

        using var scope = h.Provider.CreateScope();
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider);
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider); // second run no-ops

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Customers.CountAsync()).Should().Be(20);
        (await db.Tenants.CountAsync()).Should().Be(1);
    }
}
