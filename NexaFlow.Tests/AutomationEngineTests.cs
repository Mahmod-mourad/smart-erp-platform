using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;
using NexaFlow.Infrastructure.Services.Automation;

namespace NexaFlow.Tests;

public class AutomationEngineTests
{
    // ---- Fakes ----

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = [];
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingNotifier : IWorkflowNotifier
    {
        public List<WorkflowExecutedNotification> Notifications { get; } = [];
        public Task WorkflowExecutedAsync(Guid tenantId, WorkflowExecutedNotification payload, CancellationToken ct = default)
        {
            Notifications.Add(payload);
            return Task.CompletedTask;
        }
    }

    private sealed record Sut(
        AutomationService Service,
        AppDbContext Db,
        RecordingEmailSender Email,
        RecordingNotifier Notifier,
        Guid TenantId);

    private static Sut BuildSut(out TestHarness harness, IEnumerable<IActionHandler>? actions = null)
    {
        harness = new TestHarness();
        var tenantId = Guid.NewGuid();
        harness.TenantContext.SetTenant(tenantId);

        var db = harness.Get<AppDbContext>();
        var email = new RecordingEmailSender();
        var notifier = new RecordingNotifier();

        var triggers = new ITriggerHandler[] { new StockLowTriggerHandler(db) };
        var actionHandlers = actions ?? new IActionHandler[]
        {
            new SendEmailActionHandler(email, NullLogger<SendEmailActionHandler>.Instance)
        };

        var service = new AutomationService(
            db, harness.TenantContext, triggers, actionHandlers, notifier,
            NullLogger<AutomationService>.Instance);

        return new Sut(service, db, email, notifier, tenantId);
    }

    private static WorkflowRule AddStockLowRule(AppDbContext db, Guid tenantId, string actionsJson, int threshold = 100)
    {
        var rule = new WorkflowRule
        {
            TenantId = tenantId,
            Name = "Low stock alert",
            TriggerType = TriggerType.StockLow,
            TriggerConfig = $$"""{"threshold":{{threshold}}}""",
            ActionsConfig = actionsJson,
            IsActive = true
        };
        db.WorkflowRules.Add(rule);
        return rule;
    }

    // ---- Tests ----

    [Fact]
    public async Task TestRule_StockLow_fires_and_logs_success_when_a_product_is_below_threshold()
    {
        var sut = BuildSut(out _);
        sut.Db.Products.Add(new Product
        {
            TenantId = sut.TenantId, Name = "Laptop", CurrentStock = 5, MinimumStock = 10, IsLowStock = true
        });
        var rule = AddStockLowRule(sut.Db, sut.TenantId,
            """[{"type":"SendEmail","to":"manager@co.com","subject":"Low stock","body":"{{summary}}"}]""");
        await sut.Db.SaveChangesAsync();

        await sut.Service.TestRuleAsync(rule.Id);

        var log = await sut.Db.WorkflowLogs.SingleAsync();
        log.Status.Should().Be(WorkflowLogStatus.Success);
        log.Details.Should().Contain("Email sent to manager@co.com");
        log.TriggerData.Should().Contain("Laptop");

        sut.Email.Sent.Should().ContainSingle();
        sut.Email.Sent[0].Body.Should().Contain("Laptop"); // {{summary}} substituted
        sut.Notifier.Notifications.Should().ContainSingle()
            .Which.Status.Should().Be(nameof(WorkflowLogStatus.Success));

        (await sut.Db.WorkflowRules.SingleAsync(r => r.Id == rule.Id)).LastExecutedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TestRule_StockLow_does_not_fire_when_stock_is_healthy()
    {
        var sut = BuildSut(out _);
        sut.Db.Products.Add(new Product
        {
            TenantId = sut.TenantId, Name = "Mouse", CurrentStock = 500, MinimumStock = 10
        });
        var rule = AddStockLowRule(sut.Db, sut.TenantId,
            """[{"type":"SendEmail","to":"m@co.com","subject":"x","body":"y"}]""", threshold: 10);
        await sut.Db.SaveChangesAsync();

        await sut.Service.TestRuleAsync(rule.Id);

        (await sut.Db.WorkflowLogs.CountAsync()).Should().Be(0);
        sut.Email.Sent.Should().BeEmpty();
        sut.Notifier.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task TestRule_unknown_action_type_is_logged_as_failed()
    {
        var sut = BuildSut(out _);
        sut.Db.Products.Add(new Product
        {
            TenantId = sut.TenantId, Name = "Keyboard", CurrentStock = 1, MinimumStock = 10
        });
        var rule = AddStockLowRule(sut.Db, sut.TenantId, """[{"type":"SendCarrierPigeon","to":"x"}]""");
        await sut.Db.SaveChangesAsync();

        await sut.Service.TestRuleAsync(rule.Id);

        var log = await sut.Db.WorkflowLogs.SingleAsync();
        log.Status.Should().Be(WorkflowLogStatus.Failed);
        log.Details.Should().Contain("Unknown action type: SendCarrierPigeon");
    }

    [Fact]
    public async Task TestRule_mix_of_good_and_unknown_actions_is_partial_success()
    {
        var sut = BuildSut(out _);
        sut.Db.Products.Add(new Product
        {
            TenantId = sut.TenantId, Name = "Monitor", CurrentStock = 2, MinimumStock = 10
        });
        var rule = AddStockLowRule(sut.Db, sut.TenantId,
            """
            [{"type":"SendEmail","to":"m@co.com","subject":"Low","body":"{{summary}}"},
             {"type":"Nope"}]
            """);
        await sut.Db.SaveChangesAsync();

        await sut.Service.TestRuleAsync(rule.Id);

        var log = await sut.Db.WorkflowLogs.SingleAsync();
        log.Status.Should().Be(WorkflowLogStatus.PartialSuccess);
        sut.Email.Sent.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_then_GetRules_reports_execution_counts()
    {
        var sut = BuildSut(out _);
        var created = await sut.Service.CreateAsync(new CreateWorkflowRuleDto(
            "Daily digest", "desc", nameof(TriggerType.StockLow),
            """{"threshold":5}""",
            """[{"type":"SendEmail","to":"a@b.com","subject":"s","body":"b"}]"""));

        var rules = await sut.Service.GetRulesAsync();

        rules.Should().ContainSingle();
        rules[0].Id.Should().Be(created.Id);
        rules[0].TriggerType.Should().Be(nameof(TriggerType.StockLow));
        rules[0].TotalExecutions.Should().Be(0);
        rules[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Toggle_flips_active_flag()
    {
        var sut = BuildSut(out _);
        var created = await sut.Service.CreateAsync(new CreateWorkflowRuleDto(
            "Rule", null, nameof(TriggerType.StockLow), """{"threshold":5}""",
            """[{"type":"SendEmail","to":"a@b.com","subject":"s","body":"b"}]"""));

        var toggled = await sut.Service.ToggleAsync(created.Id);

        toggled.IsActive.Should().BeFalse();
    }
}
