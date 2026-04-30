using Invoicely.Core.Enums;

namespace Invoicely.Core.DTOs;

public record ReportQuery(
    DateTime? DateFrom,
    DateTime? DateTo,
    string? Status,
    string? PaymentStatus,
    Guid? VendorId);

public record ReportSummaryDto(
    int TotalCount,
    decimal TotalAmount,
    int PaidCount,
    decimal PaidAmount,
    int UnpaidCount,
    decimal UnpaidAmount,
    int OverdueCount,
    decimal OverdueAmount,
    List<ReportRowDto> Rows,
    List<VendorBreakdownDto> ByVendor,
    List<MonthlyBreakdownDto> ByMonth);

public record ReportRowDto(
    Guid Id,
    string InvoiceNumber,
    string VendorName,
    string CreatedByName,
    decimal Amount,
    string Currency,
    string IssueDate,
    string DueDate,
    string Status,
    string PaymentStatus,
    DateTime CreatedAt);

public record VendorBreakdownDto(
    string VendorName,
    int Count,
    decimal TotalAmount);

public record MonthlyBreakdownDto(
    string Month,
    int Count,
    decimal TotalAmount);
