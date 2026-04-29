using Invoicely.Core.Enums;

namespace Invoicely.Core.DTOs;

public record VendorDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string? TaxId,
    string? Address,
    string Status,
    int InvoiceCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateVendorRequest(
    string Name,
    string Email,
    string? Phone,
    string? TaxId,
    string? Address);

public record UpdateVendorRequest(
    string Name,
    string Email,
    string? Phone,
    string? TaxId,
    string? Address);

public record UpdateVendorStatusRequest(string Status);

public class VendorListQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
