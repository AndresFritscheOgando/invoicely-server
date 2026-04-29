using Invoicely.Core.DTOs;
using Invoicely.Core.Enums;
using Invoicely.Core.Interfaces;
using Invoicely.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Invoicely.Infrastructure.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalInvoices = await db.Invoices.CountAsync(ct);

        var pendingApproval = await db.Invoices
            .CountAsync(i => i.Status == InvoiceStatus.Submitted, ct);

        var overdueInvoices = await db.Invoices
            .CountAsync(i => i.PaymentStatus == PaymentStatus.Overdue, ct);

        var paidThisMonth = await db.Invoices
            .CountAsync(i => i.PaymentStatus == PaymentStatus.Paid && i.UpdatedAt >= startOfMonth, ct);

        var totalOutstanding = await db.Invoices
            .Where(i => i.Status == InvoiceStatus.Approved &&
                        (i.PaymentStatus == PaymentStatus.Unpaid || i.PaymentStatus == PaymentStatus.PartiallyPaid))
            .SumAsync(i => i.Amount, ct);

        var statusBreakdown = (await db.Invoices
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct))
            .Select(g => new StatusBreakdownDto { Status = g.Status.ToString(), Count = g.Count })
            .ToList();

        var sixMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var monthlySpend = (await db.Invoices
            .Where(i => i.CreatedAt >= sixMonthsAgo)
            .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(i => i.Amount) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(ct))
            .Select(g => new MonthlySpendDto
            {
                Month = $"{g.Year}-{g.Month:D2}",
                Amount = g.Amount
            })
            .ToList();

        var topVendors = await db.Invoices
            .GroupBy(i => i.Vendor.Name)
            .Select(g => new TopVendorDto
            {
                VendorName = g.Key,
                TotalAmount = g.Sum(i => i.Amount),
                InvoiceCount = g.Count()
            })
            .OrderByDescending(v => v.TotalAmount)
            .Take(5)
            .ToListAsync(ct);

        return new DashboardStatsDto
        {
            TotalInvoices = totalInvoices,
            PendingApproval = pendingApproval,
            OverdueInvoices = overdueInvoices,
            PaidThisMonth = paidThisMonth,
            TotalOutstanding = totalOutstanding,
            StatusBreakdown = statusBreakdown,
            MonthlySpend = monthlySpend,
            TopVendors = topVendors,
        };
    }
}
