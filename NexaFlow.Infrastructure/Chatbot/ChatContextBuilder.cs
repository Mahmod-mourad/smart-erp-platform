using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Chatbot;

/// <summary>
/// Builds the system prompt that grounds the AI assistant in the current tenant's live business
/// data. Every query is auto-scoped to the tenant by the DbContext global query filter, so the
/// assistant can only ever see its own company's numbers.
/// </summary>
public class ChatContextBuilder(AppDbContext db)
{
    public async Task<string> BuildContextAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var today = DateOnly.FromDateTime(now);

        var totalCustomers = await db.Customers.CountAsync(ct);
        var activeCustomers = await db.Customers.CountAsync(c => c.Status == CustomerStatus.Active, ct);

        var wonThisMonth = db.Leads.Where(l => l.Stage == LeadStage.Won && (l.UpdatedAt ?? l.CreatedAt) >= startOfMonth);
        var wonDeals = await wonThisMonth.CountAsync(ct);
        var salesThisMonth = await wonThisMonth.SumAsync(l => l.Value, ct);

        var activeEmployees = await db.Employees.CountAsync(e => e.Status == EmployeeStatus.Active, ct);
        var presentToday = await db.AttendanceRecords.CountAsync(a =>
            a.Date == today &&
            (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late), ct);

        var totalProducts = await db.Products.CountAsync(ct);
        var lowStock = await db.Products.CountAsync(p => p.IsLowStock, ct);

        var ci = CultureInfo.InvariantCulture;
        return $"""
            You are an AI business assistant for a company using the NexaFlow ERP platform.
            Today's date: {now.ToString("dddd, MMMM dd, yyyy", ci)}

            CURRENT BUSINESS DATA
            -- Customers --
            - Active customers: {activeCustomers}
            - Total customers: {totalCustomers}

            -- Sales (this month) --
            - Won deals: {wonDeals}
            - Total sales value: {salesThisMonth.ToString("N0", ci)} EGP

            -- Employees --
            - Active employees: {activeEmployees}
            - Present today: {presentToday}/{activeEmployees}

            -- Inventory --
            - Total products: {totalProducts}
            - Low-stock alerts: {lowStock}

            INSTRUCTIONS
            - Answer using only the data above.
            - If asked about data not in the context, say you don't have that information.
            - Be concise and clear; support both Arabic and English.
            - Format numbers with thousands separators.
            - Keep responses under 150 words.
            """;
    }
}
