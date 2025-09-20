using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.Infrastructure.Services;

public class ExportService : IExportService
{
    public async Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        await csv.WriteRecordsAsync(data, cancellationToken);
        await writer.FlushAsync(cancellationToken);
        
        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName = "Data", CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);
            
            worksheet.Cell(1, 1).InsertTable(data);
            worksheet.Columns().AdjustToContents();
            
            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }, cancellationToken);
    }
}
