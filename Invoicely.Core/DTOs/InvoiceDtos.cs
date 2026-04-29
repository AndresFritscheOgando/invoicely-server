using Invoicely.Core.Enums;

namespace Invoicely.Core.DTOs;

public record InvoiceItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record InvoiceItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public record InvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    Guid VendorId,
    string VendorName,
    string CreatedByName,
    decimal Amount,
    string Currency,
    string IssueDate,
    string DueDate,
    string Status,
    string PaymentStatus,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid VendorId,
    string VendorName,
    Guid CreatedByUserId,
    string CreatedByName,
    decimal Amount,
    string Currency,
    string IssueDate,
    string DueDate,
    string Status,
    string PaymentStatus,
    string? Description,
    string? FileUrl,
    IEnumerable<InvoiceItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid? ReviewedByUserId,
    string? ReviewedByName,
    DateTime? ReviewedAt,
    string? RejectionReason);

public record ApprovalActionRequest(string? RejectionReason);

public record CreateInvoiceRequest(
    Guid VendorId,
    string IssueDate,
    string DueDate,
    string? Description,
    string Currency,
    IEnumerable<InvoiceItemRequest> Items);

public record UpdateInvoiceRequest(
    Guid VendorId,
    string IssueDate,
    string DueDate,
    string? Description,
    string Currency,
    IEnumerable<InvoiceItemRequest> Items);

public class InvoiceListQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? VendorId { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
