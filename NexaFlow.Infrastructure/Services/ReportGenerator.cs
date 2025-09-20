using NexaFlow.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexaFlow.Infrastructure.Services;

public class ReportGenerator : IReportGenerator
{
    public ReportGenerator()
    {
        // Must be called once before generating any document.
        // Handled in Program.cs globally, but safe here if missed.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(string invoiceNumber, string customerName, decimal totalAmount, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            // Standard A4 page, RTL for Arabic support
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Arial));
                page.ContentFromRightToLeft(); // RTL

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, invoiceNumber, customerName, totalAmount));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        // Use Task.Run to offload PDF rendering from thread pool if it's heavy
        return await Task.Run(() => document.GeneratePdf(), cancellationToken);
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("فاتورة ضريبية").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("NexaFlow ERP").FontSize(14).FontColor(Colors.Grey.Medium);
                column.Item().Text(DateTime.UtcNow.ToString("d")).FontSize(10);
            });
        });
    }

    private void ComposeContent(IContainer container, string invoiceNumber, string customerName, decimal totalAmount)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(20);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"رقم الفاتورة: {invoiceNumber}");
                row.RelativeItem().Text($"اسم العميل: {customerName}");
            });

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Description
                    columns.RelativeColumn();  // Qty
                    columns.RelativeColumn();  // Price
                    columns.RelativeColumn();  // Total
                });

                table.Header(header =>
                {
                    header.Cell().Text("الوصف").SemiBold();
                    header.Cell().Text("الكمية").SemiBold();
                    header.Cell().Text("السعر").SemiBold();
                    header.Cell().Text("الإجمالي").SemiBold();
                });

                // Mock single line item for demo
                table.Cell().Text("خدمات برمجية");
                table.Cell().Text("1");
                table.Cell().Text(totalAmount.ToString("C"));
                table.Cell().Text(totalAmount.ToString("C"));
            });

            column.Item().AlignRight().Text($"الإجمالي الكلي: {totalAmount:C}").FontSize(14).SemiBold();
        });
    }

    public Task<byte[]> GenerateJournalEntryPdfAsync(Guid journalId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
