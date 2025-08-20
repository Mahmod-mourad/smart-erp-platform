namespace NexaFlow.Application.Common.Interfaces;

public interface IReportGenerator
{
    Task<byte[]> GenerateInvoicePdfAsync(string invoiceNumber, string customerName, decimal totalAmount, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateJournalEntryPdfAsync(Guid journalId, CancellationToken cancellationToken = default);
}
