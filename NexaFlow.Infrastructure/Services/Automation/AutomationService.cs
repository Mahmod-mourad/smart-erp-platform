using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>
/// Orchestrates the automation engine: CRUD for rules/logs plus evaluation of active rules.
/// Evaluation relies on the ambient <see cref="ITenantContext"/> (set by middleware for HTTP
/// requests, or by the Hangfire job per tenant), so the same code path works in both contexts.
/// </summary>
public class AutomationService(
    AppDbContext db,
    ITenantContext tenant,
    IEnumerable<ITriggerHandler> triggerHandlers,
    IEnumerable<IActionHandler> actionHandlers,
    IWorkflowNotifier notifier,
    ILogger<AutomationService> logger) : IAutomationService
{
    private Guid TenantId => tenant.TenantId
        ?? throw new UnauthorizedAppException("No tenant in the current context.");

    // ---- CRUD ----

    public async Task<IReadOnlyList<WorkflowRuleDto>> GetRulesAsync(CancellationToken ct = default)
    {
        var rows = await db.WorkflowRules
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RuleWithCounts(
                r,
                db.WorkflowLogs.Count(l => l.RuleId == r.Id),
                db.WorkflowLogs.Count(l => l.RuleId == r.Id && l.Status == WorkflowLogStatus.Success)))
            .ToListAsync(ct);

        return rows.Select(ToDto).ToList();
    }

    public async Task<WorkflowRuleDto> GetRuleAsync(Guid id, CancellationToken ct = default)
    {
        var row = await db.WorkflowRules
            .Where(r => r.Id == id)
            .Select(r => new RuleWithCounts(
                r,
                db.WorkflowLogs.Count(l => l.RuleId == r.Id),
                db.WorkflowLogs.Count(l => l.RuleId == r.Id && l.Status == WorkflowLogStatus.Success)))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Workflow rule not found.");

        return ToDto(row);
    }

    public async Task<WorkflowRuleDto> CreateAsync(CreateWorkflowRuleDto request, CancellationToken ct = default)
    {
        var rule = new WorkflowRule
        {
            TenantId = TenantId,
            Name = request.Name,
            Description = request.Description,
            TriggerType = Enum.Parse<TriggerType>(request.TriggerType),
            TriggerConfig = request.TriggerConfig,
            ActionsConfig = request.ActionsConfig,
            IsActive = true
        };

        db.WorkflowRules.Add(rule);
        await db.SaveChangesAsync(ct);

        return ToDto(new RuleWithCounts(rule, 0, 0));
    }

    public async Task<WorkflowRuleDto> UpdateAsync(Guid id, UpdateWorkflowRuleDto request, CancellationToken ct = default)
    {
        var rule = await db.WorkflowRules.FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw new NotFoundException("Workflow rule not found.");

        rule.Name = request.Name;
        rule.Description = request.Description;
        rule.TriggerType = Enum.Parse<TriggerType>(request.TriggerType);
        rule.TriggerConfig = request.TriggerConfig;
        rule.ActionsConfig = request.ActionsConfig;

        await db.SaveChangesAsync(ct);
        return await GetRuleAsync(id, ct);
    }

    public async Task<WorkflowRuleDto> ToggleAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await db.WorkflowRules.FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw new NotFoundException("Workflow rule not found.");

        rule.IsActive = !rule.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetRuleAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await db.WorkflowRules.FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw new NotFoundException("Workflow rule not found.");
        db.WorkflowRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }

    public async Task<WorkflowLogPageDto> GetLogsAsync(Guid ruleId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var ruleExists = await db.WorkflowRules.AnyAsync(r => r.Id == ruleId, ct);
        if (!ruleExists)
            throw new NotFoundException("Workflow rule not found.");

        var query = db.WorkflowLogs.Where(l => l.RuleId == ruleId);
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new WorkflowLogDto(
                l.Id, l.Rule.Name, l.ExecutedAt, l.Status.ToString(), l.Details, l.TriggerData))
            .ToListAsync(ct);

        return new WorkflowLogPageDto(items, total, page, pageSize);
    }

    // ---- Engine ----

    public async Task TestRuleAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await db.WorkflowRules.FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw new NotFoundException("Workflow rule not found.");
        await EvaluateRuleAsync(rule, ct);
    }

    public async Task EvaluateActiveRulesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Automation: evaluating active rules for tenant {TenantId}", TenantId);

        var rules = await db.WorkflowRules.Where(r => r.IsActive).ToListAsync(ct);
        foreach (var rule in rules)
            await EvaluateRuleAsync(rule, ct);
    }

    private async Task EvaluateRuleAsync(WorkflowRule rule, CancellationToken ct)
    {
        try
        {
            var triggerHandler = triggerHandlers.FirstOrDefault(h => h.TriggerType == rule.TriggerType);
            if (triggerHandler is null)
            {
                logger.LogWarning("Automation: no handler for trigger type {TriggerType}", rule.TriggerType);
                return;
            }

            var triggerResult = await triggerHandler.EvaluateAsync(rule, ct);
            if (triggerResult is null)
                return; // condition not met — nothing to do

            logger.LogInformation("Automation: rule '{RuleName}' fired — {Summary}", rule.Name, triggerResult.Summary);

            var lines = await RunActionsAsync(rule, triggerResult, ct);
            var status = DetermineStatus(lines);

            var log = new WorkflowLog
            {
                TenantId = rule.TenantId,
                RuleId = rule.Id,
                ExecutedAt = DateTime.UtcNow,
                Status = status,
                Details = string.Join("\n", lines.Select(l => l.Text)),
                TriggerData = triggerResult.Summary
            };

            db.WorkflowLogs.Add(log);
            rule.LastExecutedAt = log.ExecutedAt;
            await db.SaveChangesAsync(ct);

            await notifier.WorkflowExecutedAsync(rule.TenantId,
                new WorkflowExecutedNotification(rule.Name, status.ToString(), triggerResult.Summary, log.ExecutedAt),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automation: error evaluating rule '{RuleName}'", rule.Name);
        }
    }

    private async Task<List<ActionLine>> RunActionsAsync(WorkflowRule rule, TriggerResult triggerResult, CancellationToken ct)
    {
        var lines = new List<ActionLine>();

        List<JsonElement> actions;
        try
        {
            actions = JsonSerializer.Deserialize<List<JsonElement>>(rule.ActionsConfig, AutomationJson.Options) ?? [];
        }
        catch (JsonException)
        {
            lines.Add(new ActionLine(false, "❌ Invalid actions configuration (not valid JSON)"));
            return lines;
        }

        foreach (var element in actions)
        {
            if (!element.TryGetProperty("type", out var typeProp) || typeProp.GetString() is not { } actionType)
            {
                lines.Add(new ActionLine(false, "❌ Action missing 'type'"));
                continue;
            }

            var handler = actionHandlers.FirstOrDefault(h => h.ActionType == actionType);
            if (handler is null)
            {
                lines.Add(new ActionLine(false, $"❌ Unknown action type: {actionType}"));
                continue;
            }

            var result = await handler.ExecuteAsync(element.GetRawText(), triggerResult, rule.TenantId, ct);
            lines.Add(new ActionLine(result.Success, $"{(result.Success ? "✅" : "❌")} {result.Message}"));
        }

        return lines;
    }

    private static WorkflowLogStatus DetermineStatus(IReadOnlyCollection<ActionLine> lines)
    {
        if (lines.Count == 0 || lines.All(l => l.Success)) return WorkflowLogStatus.Success;
        if (lines.All(l => !l.Success)) return WorkflowLogStatus.Failed;
        return WorkflowLogStatus.PartialSuccess;
    }

    private static WorkflowRuleDto ToDto(RuleWithCounts row) => new(
        row.Rule.Id, row.Rule.Name, row.Rule.Description,
        row.Rule.TriggerType.ToString(), row.Rule.TriggerConfig, row.Rule.ActionsConfig,
        row.Rule.IsActive, row.Rule.LastExecutedAt, row.Total, row.Successful, row.Rule.CreatedAt);

    private sealed record RuleWithCounts(WorkflowRule Rule, int Total, int Successful);
    private sealed record ActionLine(bool Success, string Text);
}
