using System.Text;
using Invoicely.Core.DTOs;
using Invoicely.Core.Enums;
using Invoicely.Core.Interfaces;
using Invoicely.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Invoicely.Infrastructure.Services;

public class ReportService(AppDbContext db) : IReportService
{
    public async Task<ReportSummaryDto> GetSummaryAsync(ReportQuery query, CancellationToken ct = default)
    {
        var q = db.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.CreatedBy)
            .AsNoTracking()
            .AsQueryable();

        q = ApplyFilters(q, query);

        var invoices = await q
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        var rows = invoices.Select(i => new ReportRowDto(
            i.Id,
            i.InvoiceNumber,
            i.Vendor.Name,
            i.CreatedBy.Name,
            i.Amount,
            i.Currency,
            i.IssueDate.ToString("yyyy-MM-dd"),
            i.DueDate.ToString("yyyy-MM-dd"),
            i.Status.ToString(),
            i.PaymentStatus.ToString(),
            i.CreatedAt)).ToList();

        var paid = invoices.Where(i => i.PaymentStatus == PaymentStatus.Paid).ToList();
        var unpaid = invoices.Where(i => i.PaymentStatus == PaymentStatus.Unpaid || i.PaymentStatus == PaymentStatus.PartiallyPaid).ToList();
        var overdue = invoices.Where(i => i.PaymentStatus == PaymentStatus.Overdue).ToList();

        var byVendor = invoices
            .GroupBy(i => i.Vendor.Name)
            .Select(g => new VendorBreakdownDto(g.Key, g.Count(), g.Sum(i => i.Amount)))
            .OrderByDescending(v => v.TotalAmount)
            .Take(10)
            .ToList();

        var byMonth = invoices
            .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyBreakdownDto(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Count(),
                g.Sum(i => i.Amount)))
            .ToList();

        return new ReportSummaryDto(
            invoices.Count,
            invoices.Sum(i => i.Amount),
            paid.Count,
            paid.Sum(i => i.Amount),
            unpaid.Count,
            unpaid.Sum(i => i.Amount),
            overdue.Count,
            overdue.Sum(i => i.Amount),
            rows,
            byVendor,
            byMonth);
    }

    public async Task<string> ExportCsvAsync(ReportQuery query, CancellationToken ct = default)
    {
        var q = db.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.CreatedBy)
            .AsNoTracking()
            .AsQueryable();

        q = ApplyFilters(q, query);

        var invoices = await q.OrderByDescending(i => i.CreatedAt).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Invoice Number,Vendor,Created By,Amount,Currency,Issue Date,Due Date,Status,Payment Status,Created At");

        foreach (var i in invoices)
        {
            sb.AppendLine(string.Join(",",
                Escape(i.InvoiceNumber),
                Escape(i.Vendor.Name),
                Escape(i.CreatedBy.Name),
                i.Amount.ToString("F2"),
                Escape(i.Currency),
                i.IssueDate.ToString("yyyy-MM-dd"),
                i.DueDate.ToString("yyyy-MM-dd"),
                i.Status.ToString(),
                i.PaymentStatus.ToString(),
                i.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        return sb.ToString();
    }

    private static IQueryable<Core.Entities.Invoice> ApplyFilters(
        IQueryable<Core.Entities.Invoice> q,
        ReportQuery query)
    {
        if (query.DateFrom.HasValue)
            q = q.Where(i => i.CreatedAt >= query.DateFrom.Value.ToUniversalTime());

        if (query.DateTo.HasValue)
            q = q.Where(i => i.CreatedAt <= query.DateTo.Value.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<InvoiceStatus>(query.Status, out var status))
            q = q.Where(i => i.Status == status);

        if (!string.IsNullOrWhiteSpace(query.PaymentStatus) &&
            Enum.TryParse<PaymentStatus>(query.PaymentStatus, out var paymentStatus))
            q = q.Where(i => i.PaymentStatus == paymentStatus);

        if (query.VendorId.HasValue)
            q = q.Where(i => i.VendorId == query.VendorId.Value);

        return q;
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
