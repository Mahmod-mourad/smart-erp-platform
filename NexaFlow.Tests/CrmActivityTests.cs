using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.DTOs;
using NexaFlow.Infrastructure.Persistence;
using NexaFlow.Infrastructure.Services;

namespace NexaFlow.Tests;

public class CrmActivityTests
{
    /// <summary>Wires the three CRM services over one scoped DbContext for the given tenant.</summary>
    private static (AppDbContext db, CustomerService customers, LeadService leads, ActivityService activities)
        ArrangeFor(TestHarness h, Guid tenantId, IServiceScope scope)
    {
        h.CurrentUser.TenantId = tenantId;
        h.CurrentUser.UserId = Guid.NewGuid();
        h.TenantContext.SetTenant(tenantId);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (db, new CustomerService(db, h.CurrentUser), new LeadService(db, h.CurrentUser), new ActivityService(db, h.CurrentUser));
    }

    [Fact]
    public async Task Creating_a_customer_logs_a_system_activity()
    {
        var h = new TestHarness();
        using var scope = h.Provider.CreateScope();
        var (_, customers, _, activities) = ArrangeFor(h, Guid.NewGuid(), scope);

        var customer = await customers.CreateAsync(new CreateCustomerDto("Acme", null, null, null, null, null));

        var timeline = await activities.GetForCustomerAsync(customer.Id);
        timeline.Should().ContainSingle();
        timeline[0].Type.Should().Be("StatusChange");
        timeline[0].Content.Should().Be("Customer created.");
    }

    [Fact]
    public async Task Moving_a_lead_stage_appends_a_timeline_entry()
    {
        var h = new TestHarness();
        using var scope = h.Provider.CreateScope();
        var (_, customers, leads, activities) = ArrangeFor(h, Guid.NewGuid(), scope);

        var customer = await customers.CreateAsync(new CreateCustomerDto("Acme", null, null, null, null, null));
        var lead = await leads.CreateAsync(new CreateLeadDto("Big deal", 1000m, customer.Id, null, null));

        await leads.UpdateStageAsync(lead.Id, new UpdateLeadStageDto("Negotiation"));

        var timeline = await activities.GetForCustomerAsync(customer.Id);
        timeline.Should().HaveCount(3); // customer created + lead created + stage moved
        timeline.Should().Contain(a => a.Content.Contains("moved from Prospect to Negotiation"));
    }

    [Fact]
    public async Task Activities_are_isolated_per_tenant()
    {
        var h = new TestHarness();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        Guid customerId;
        using (var scope = h.Provider.CreateScope())
        {
            var (_, customers, _, _) = ArrangeFor(h, tenantA, scope);
            var customer = await customers.CreateAsync(new CreateCustomerDto("Acme", null, null, null, null, null));
            customerId = customer.Id;
        }

        // Acting as tenant B, tenant A's customer (and its timeline) must be invisible.
        using (var scope = h.Provider.CreateScope())
        {
            var (db, _, _, activities) = ArrangeFor(h, tenantB, scope);

            (await db.Activities.CountAsync()).Should().Be(0);
            var act = async () => await activities.GetForCustomerAsync(customerId);
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
