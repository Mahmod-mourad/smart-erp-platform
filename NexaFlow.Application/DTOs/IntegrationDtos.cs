namespace NexaFlow.Application.DTOs;

// IntegrationType is exposed/accepted as its string name ("WhatsApp" | "Gmail" | ...), matching the
// project convention of stringifying enums across the API boundary.

/// <summary>An integration's status for the settings UI. Deliberately carries NO credentials.</summary>
public record IntegrationDto(
    string Type,
    bool IsEnabled,
    bool IsConfigured,
    DateTime? LastTestedAt,
    bool? LastTestSuccess);

/// <summary>
/// Saves/updates an integration. <see cref="Config"/> holds the credential fields keyed by name
/// (e.g. "accessToken", "phoneNumberId"). A blank/omitted secret keeps the previously stored value.
/// </summary>
public record UpsertIntegrationDto(
    bool IsEnabled,
    Dictionary<string, string> Config);

public record IntegrationTestResultDto(
    bool Success,
    string Message);

// ---- Strongly-typed integration configs (serialized into TenantIntegration.EncryptedConfig) ----

public record WhatsAppConfig(string AccessToken, string PhoneNumberId);
public record GmailConfig(string Email, string AppPassword, string? FromName);
public record SlackConfig(string WebhookUrl, string? DefaultChannel);
public record GoogleSheetsConfig(string ServiceAccountJson, string SpreadsheetId, string? SheetName);
