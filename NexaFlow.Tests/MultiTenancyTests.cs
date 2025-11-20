using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Infrastructure.Identity;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Tests;

public class MultiTenancyTests
{
    [Fact]
    public async Task Tenant_A_cannot_see_Tenant_B_users_via_global_query_filter()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();

        var a = await auth.RegisterCompanyAsync(new("Alpha", "Al", "Pha", "admin@alpha.com", "Sup3rSecret!"), null);
        var b = await auth.RegisterCompanyAsync(new("Beta", "Be", "Ta", "admin@beta.com", "Sup3rSecret!"), null);

        // Acting as tenant A, the filtered query must only return A's users.
        h.TenantContext.SetTenant(a.User.TenantId);
        using (var scope = h.Provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emails = await db.Users.Select(u => u.Email).ToListAsync();
            emails.Should().ContainSingle().Which.Should().Be("admin@alpha.com");
        }

        // Switching to tenant B flips the visible set.
        h.TenantContext.SetTenant(b.User.TenantId);
        using (var scope = h.Provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emails = await db.Users.Select(u => u.Email).ToListAsync();
            emails.Should().ContainSingle().Which.Should().Be("admin@beta.com");
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_reveals_all_tenants_users()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();
        await auth.RegisterCompanyAsync(new("Alpha", "Al", "Pha", "admin@alpha.com", "Sup3rSecret!"), null);
        await auth.RegisterCompanyAsync(new("Beta", "Be", "Ta", "admin@beta.com", "Sup3rSecret!"), null);

        h.TenantContext.SetTenant(Guid.NewGuid()); // some unrelated tenant
        using var scope = h.Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await db.Users.CountAsync()).Should().Be(0);                       // filtered out
        (await db.Users.IgnoreQueryFilters().CountAsync()).Should().Be(2);  // raw
    }
}

public class TenantServiceTests
{
    [Fact]
    public async Task GetCurrent_returns_the_callers_tenant()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();
        var reg = await auth.RegisterCompanyAsync(new("Gamma", "Ga", "Mma", "admin@gamma.com", "Sup3rSecret!"), null);

        h.CurrentUser.TenantId = reg.User.TenantId;
        var tenants = h.Get<ITenantService>();

        var dto = await tenants.GetCurrentAsync();
        dto.Name.Should().Be("Gamma");
        dto.Slug.Should().Be("gamma");
    }
}
