using Invoicely.Core.DTOs;

namespace Invoicely.Core.Interfaces;

public interface IInvoiceService
{
    Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesAsync(InvoiceListQuery query, CancellationToken ct = default);
    Task<InvoiceDto> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, Guid userId, CancellationToken ct = default);
    Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceRequest request, Guid userId, CancellationToken ct = default);
    Task DeleteInvoiceAsync(Guid id, Guid userId, string userRole, CancellationToken ct = default);
    Task<InvoiceDto> SubmitInvoiceAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<InvoiceDto> ApproveInvoiceAsync(Guid id, Guid reviewerId, CancellationToken ct = default);
    Task<InvoiceDto> RejectInvoiceAsync(Guid id, Guid reviewerId, string reason, CancellationToken ct = default);
    Task<InvoiceDto> CancelInvoiceAsync(Guid id, Guid userId, string userRole, CancellationToken ct = default);
}
