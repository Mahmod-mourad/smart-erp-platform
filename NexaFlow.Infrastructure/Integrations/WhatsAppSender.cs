using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Infrastructure.Integrations;

/// <summary>
/// Sends WhatsApp messages through the Meta WhatsApp Business Cloud API using the calling tenant's
/// stored credentials. When the tenant hasn't configured/enabled WhatsApp it logs and returns
/// without throwing, so the automation engine keeps running.
/// </summary>
public class WhatsAppSender(
    HttpClient http,
    IIntegrationConfigProvider configProvider,
    ILogger<WhatsAppSender> logger) : IWhatsAppSender
{
    private const string BaseUrl = "https://graph.facebook.com/v18.0";

    public async Task SendMessageAsync(string to, string message, CancellationToken ct = default)
    {
        var config = await configProvider.GetConfigAsync<WhatsAppConfig>(IntegrationType.WhatsApp, ct);
        if (config is null || string.IsNullOrWhiteSpace(config.AccessToken) || string.IsNullOrWhiteSpace(config.PhoneNumberId))
        {
            logger.LogInformation("💬 [WHATSAPP SKIPPED — not configured] To: {To}\n{Message}", to, message);
            return;
        }

        var phone = to.StartsWith('+') ? to[1..] : to;
        var payload = new
        {
            messaging_product = "whatsapp",
            to = phone,
            type = "text",
            text = new { body = message }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{config.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new("Bearer", config.AccessToken);

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("WhatsApp API failed: {StatusCode} - {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"WhatsApp API returned {(int)response.StatusCode}.");
        }

        logger.LogInformation("WhatsApp message sent to {Phone}", phone);
    }
}
