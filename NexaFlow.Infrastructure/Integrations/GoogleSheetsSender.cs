using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Infrastructure.Integrations;

/// <summary>
/// Appends rows to the tenant's configured Google Sheet using a service account. Skips (logs only)
/// when Google Sheets isn't configured/enabled for the tenant.
/// </summary>
public class GoogleSheetsSender(
    IIntegrationConfigProvider configProvider,
    ILogger<GoogleSheetsSender> logger) : IGoogleSheetsSender
{
    public async Task AppendRowAsync(IEnumerable<string> values, CancellationToken ct = default)
    {
        var config = await configProvider.GetConfigAsync<GoogleSheetsConfig>(IntegrationType.GoogleSheets, ct);
        if (config is null || string.IsNullOrWhiteSpace(config.ServiceAccountJson) || string.IsNullOrWhiteSpace(config.SpreadsheetId))
        {
            logger.LogInformation("📊 [GOOGLE SHEETS SKIPPED — not configured]");
            return;
        }

        // FromJson is flagged obsolete over confused-deputy risk with untrusted JSON; here the JSON
        // is the tenant's own service-account key, decrypted from our store, so the risk doesn't apply.
#pragma warning disable CS0618
        var credential = GoogleCredential
            .FromJson(config.ServiceAccountJson)
            .CreateScoped(SheetsService.Scope.Spreadsheets);
#pragma warning restore CS0618

        using var service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "NexaFlow"
        });

        var range = string.IsNullOrWhiteSpace(config.SheetName) ? "A1" : $"{config.SheetName}!A1";
        var body = new ValueRange { Values = [values.Cast<object>().ToList()] };
        var request = service.Spreadsheets.Values.Append(body, config.SpreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

        await request.ExecuteAsync(ct);
        logger.LogInformation("Appended a row to Google Sheet {SpreadsheetId}", config.SpreadsheetId);
    }
}
