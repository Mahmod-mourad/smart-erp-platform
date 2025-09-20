using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Integrations;

/// <summary>
/// Manages the current tenant's integration connections. Credentials are encrypted before they're
/// stored and are never returned to callers; the test action drives a real message through the
/// corresponding sender and records the outcome.
/// </summary>
public class IntegrationService(
    AppDbContext db,
    ICurrentUser currentUser,
    IntegrationCrypto crypto,
    ISlackSender slack,
    IEmailSender email,
    IGoogleSheetsSender sheets,
    IHttpClientFactory httpFactory,
    ILogger<IntegrationService> logger) : IIntegrationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<IntegrationDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.TenantIntegrations.AsNoTracking().ToListAsync(ct);
        var byType = rows.ToDictionary(r => r.Type);

        // Always surface all four integration types so the UI can render every card.
        return Enum.GetValues<IntegrationType>()
            .Select(type =>
            {
                byType.TryGetValue(type, out var row);
                return new IntegrationDto(
                    type.ToString(),
                    row?.IsEnabled ?? false,
                    !string.IsNullOrEmpty(row?.EncryptedConfig),
                    row?.LastTestedAt,
                    row?.LastTestSuccess);
            })
            .ToList();
    }

    public async Task<IntegrationDto> UpsertAsync(IntegrationType type, UpsertIntegrationDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var row = await db.TenantIntegrations.FirstOrDefaultAsync(i => i.Type == type, ct);

        // Merge over the existing config so a blank/omitted secret keeps its stored value.
        var merged = row is null || string.IsNullOrEmpty(row.EncryptedConfig)
            ? new Dictionary<string, string>()
            : Deserialize(crypto.Unprotect(row.EncryptedConfig));

        foreach (var (key, value) in request.Config)
        {
            if (!string.IsNullOrWhiteSpace(value))
                merged[key] = value;
        }

        var encrypted = crypto.Protect(JsonSerializer.Serialize(merged, JsonOptions));

        if (row is null)
        {
            row = new TenantIntegration { TenantId = tenantId, Type = type };
            db.TenantIntegrations.Add(row);
        }

        row.IsEnabled = request.IsEnabled;
        row.EncryptedConfig = encrypted;
        await db.SaveChangesAsync(ct);

        return new IntegrationDto(type.ToString(), row.IsEnabled, true, row.LastTestedAt, row.LastTestSuccess);
    }

    public async Task<IntegrationTestResultDto> TestAsync(IntegrationType type, CancellationToken ct = default)
    {
        var row = await db.TenantIntegrations.FirstOrDefaultAsync(i => i.Type == type, ct);
        if (row is null || string.IsNullOrEmpty(row.EncryptedConfig))
            return new IntegrationTestResultDto(false, "This integration is not configured yet.");
        if (!row.IsEnabled)
            return new IntegrationTestResultDto(false, "Enable the integration before testing it.");

        IntegrationTestResultDto result;
        try
        {
            result = await RunTestAsync(type, row, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Integration test failed for {Type}", type);
            result = new IntegrationTestResultDto(false, ex.Message);
        }

        row.LastTestedAt = DateTime.UtcNow;
        row.LastTestSuccess = result.Success;
        await db.SaveChangesAsync(ct);
        return result;
    }

    private async Task<IntegrationTestResultDto> RunTestAsync(IntegrationType type, TenantIntegration row, CancellationToken ct)
    {
        switch (type)
        {
            case IntegrationType.Slack:
                await slack.SendAsync("✅ NexaFlow connected to Slack successfully.", null, ct);
                return new IntegrationTestResultDto(true, "Test message sent to your Slack channel.");

            case IntegrationType.Gmail:
                var to = currentUser.Email ?? throw new InvalidOperationException("No admin email on the current user.");
                await email.SendAsync(to, "NexaFlow test email",
                    "<p>✅ Your Gmail integration is working.</p>", ct);
                return new IntegrationTestResultDto(true, $"Test email sent to {to}.");

            case IntegrationType.GoogleSheets:
                await sheets.AppendRowAsync(["NexaFlow test", DateTime.UtcNow.ToString("u")], ct);
                return new IntegrationTestResultDto(true, "A test row was appended to your sheet.");

            case IntegrationType.WhatsApp:
                return await TestWhatsAppAsync(row, ct);

            default:
                return new IntegrationTestResultDto(false, "Unsupported integration type.");
        }
    }

    // WhatsApp has no test recipient in its config, so validate the credentials by reading the
    // phone-number resource from the Meta Graph API instead of sending to an unknown number.
    private async Task<IntegrationTestResultDto> TestWhatsAppAsync(TenantIntegration row, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<WhatsAppConfig>(crypto.Unprotect(row.EncryptedConfig), JsonOptions);
        if (config is null || string.IsNullOrWhiteSpace(config.AccessToken) || string.IsNullOrWhiteSpace(config.PhoneNumberId))
            return new IntegrationTestResultDto(false, "WhatsApp credentials are incomplete.");

        var http = httpFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://graph.facebook.com/v18.0/{config.PhoneNumberId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);

        var response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode
            ? new IntegrationTestResultDto(true, "WhatsApp credentials verified with Meta.")
            : new IntegrationTestResultDto(false, $"Meta API returned {(int)response.StatusCode}. Check your token and phone number ID.");
    }

    private static Dictionary<string, string> Deserialize(string json)
        => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new();
}
