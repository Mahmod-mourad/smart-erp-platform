using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>
/// Per-tenant credentials/settings for a third-party integration (WhatsApp, Gmail, Slack, Google
/// Sheets). The credential JSON is encrypted at rest in <see cref="EncryptedConfig"/> via ASP.NET
/// Data Protection — secrets are never stored in plaintext nor returned to the client.
/// </summary>
public class TenantIntegration : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Which integration this row configures. One row per (tenant, type).</summary>
    public IntegrationType Type { get; set; }

    /// <summary>When false the integration is configured but the senders skip it.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Data-Protection ciphertext of the integration's credential JSON. Empty until configured.</summary>
    public string EncryptedConfig { get; set; } = string.Empty;

    /// <summary>When the connection was last tested via the "Test" action.</summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>Outcome of the last test, or null if never tested.</summary>
    public bool? LastTestSuccess { get; set; }
}
