using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Infrastructure.Integrations;

/// <summary>
/// Posts messages to the tenant's configured Slack incoming webhook. Skips (logs only) when Slack
/// isn't configured/enabled for the tenant.
/// </summary>
public class SlackWebhookSender(
    HttpClient http,
    IIntegrationConfigProvider configProvider,
    ILogger<SlackWebhookSender> logger) : ISlackSender
{
    public async Task SendAsync(string message, string? channel, CancellationToken ct = default)
    {
        var config = await configProvider.GetConfigAsync<SlackConfig>(IntegrationType.Slack, ct);
        if (config is null || string.IsNullOrWhiteSpace(config.WebhookUrl))
        {
            logger.LogInformation("💼 [SLACK SKIPPED — not configured] Channel: {Channel}\n{Message}",
                channel ?? config?.DefaultChannel ?? "(default)", message);
            return;
        }

        var payload = new
        {
            text = message,
            channel = channel ?? config.DefaultChannel
        };

        var response = await http.PostAsJsonAsync(config.WebhookUrl, payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Slack webhook failed: {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"Slack webhook returned {(int)response.StatusCode}.");
        }

        logger.LogInformation("Slack message posted");
    }
}
