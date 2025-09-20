using NexaFlow.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexaFlow.Infrastructure.PDF;

/// <summary>Renders a single employee payslip to PDF via QuestPDF.</summary>
public class PayslipDocument(PayslipDto payslip) : IDocument
{
    private readonly PayslipDto _p = payslip;

    public DocumentMetadata GetMetadata() => new() { Title = $"Payslip - {MonthLabel(_p)}" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(t => t.FontSize(11));

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("NexaFlow").Bold().FontSize(22);
                    col.Item().Text("Employee Payslip").FontSize(14).FontColor("#666666");
                });
                row.ConstantItem(200).Column(col =>
                {
                    col.Item().AlignRight().Text($"Month: {MonthLabel(_p)}").FontSize(12);
                    col.Item().AlignRight().Text($"Generated: {DateTime.UtcNow:dd/MM/yyyy}").FontSize(10);
                });
            });

            page.Content().PaddingTop(20).Column(col =>
            {
                col.Item().Background("#F5F5F5").Padding(10).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                    InfoCell(table, "Full Name", _p.EmployeeFullName);
                    InfoCell(table, "Department", _p.Department);
                    InfoCell(table, "Position", _p.Position);
                });

                col.Item().PaddingTop(15).Text("Salary Breakdown").Bold().FontSize(14);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                    MoneyRow(table, "Base Salary", _p.BaseSalary);
                    MoneyRow(table, "Allowances", _p.Allowances);
                    MoneyRow(table, "Gross Salary", _p.GrossSalary);
                    MoneyRow(table, "Absence Deduction", -_p.AbsenceDeduction);
                    MoneyRow(table, "NET SALARY", _p.NetSalary, bold: true);
                });

                col.Item().PaddingTop(15).Text("Attendance Summary").Bold().FontSize(14);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                    });
                    SummaryHeader(table, "Present", "Absent", "Leave", "Working Days");
                    SummaryValue(table,
                        _p.PresentDays.ToString(), _p.AbsentDays.ToString(),
                        _p.LeaveDays.ToString(), _p.WorkingDays.ToString());
                });
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("This is a system-generated payslip. ").FontSize(8).FontColor("#999999");
                t.Span($"Daily rate: {_p.DailyRate:N2}").FontSize(8).FontColor("#999999");
            });
        });
    }

    private static string MonthLabel(PayslipDto p) =>
        p.Month.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy");

    private static void InfoCell(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(2).Text(label).SemiBold();
        table.Cell().PaddingVertical(2).Text(value);
    }

    private static void MoneyRow(TableDescriptor table, string label, decimal amount, bool bold = false)
    {
        var labelCell = table.Cell().BorderBottom(0.5f).BorderColor("#E0E0E0").PaddingVertical(4).Text(label);
        var amountCell = table.Cell().BorderBottom(0.5f).BorderColor("#E0E0E0").PaddingVertical(4)
            .AlignRight().Text($"{amount:N2}");
        if (bold)
        {
            labelCell.Bold().FontSize(13);
            amountCell.Bold().FontSize(13);
        }
    }

    private static void SummaryHeader(TableDescriptor table, params string[] headers)
    {
        foreach (var h in headers)
            table.Cell().Background("#F5F5F5").Padding(5).AlignCenter().Text(h).SemiBold();
    }

    private static void SummaryValue(TableDescriptor table, params string[] values)
    {
        foreach (var v in values)
            table.Cell().Padding(5).AlignCenter().Text(v);
    }
}
