namespace Invoicely.Core.DTOs;

public class DashboardStatsDto
{
    public int TotalInvoices { get; set; }
    public int PendingApproval { get; set; }
    public int OverdueInvoices { get; set; }
    public int PaidThisMonth { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<StatusBreakdownDto> StatusBreakdown { get; set; } = [];
    public List<MonthlySpendDto> MonthlySpend { get; set; } = [];
    public List<TopVendorDto> TopVendors { get; set; } = [];
}

public class StatusBreakdownDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlySpendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TopVendorDto
{
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InvoiceCount { get; set; }
}
