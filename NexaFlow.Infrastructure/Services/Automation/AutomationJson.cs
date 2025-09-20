using System.Text.Json;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>
/// Shared JSON options for (de)serializing trigger/action configs. The Angular builder emits
/// camelCase keys, so matching is case-insensitive and enums are read by name.
/// </summary>
internal static class AutomationJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}
