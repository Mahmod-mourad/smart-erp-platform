using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Integrations;

/// <summary>
/// Resolves the decrypted, typed config for an integration belonging to the ambient tenant. Returns
/// null when the integration row is missing, disabled, or has no stored config — letting senders
/// degrade gracefully instead of throwing. Reads scope to the tenant via the global query filter.
/// </summary>
public class IntegrationConfigProvider(
    AppDbContext db,
    IntegrationCrypto crypto,
    ILogger<IntegrationConfigProvider> logger) : IIntegrationConfigProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetConfigAsync<T>(IntegrationType type, CancellationToken ct = default) where T : class
    {
        var row = await db.TenantIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Type == type, ct);

        if (row is null || !row.IsEnabled || string.IsNullOrEmpty(row.EncryptedConfig))
            return null;

        try
        {
            var json = crypto.Unprotect(row.EncryptedConfig);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read {Type} integration config for the current tenant", type);
            return null;
        }
    }
}
