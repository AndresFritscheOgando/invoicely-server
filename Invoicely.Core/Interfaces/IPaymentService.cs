using Invoicely.Core.DTOs;

namespace Invoicely.Core.Interfaces;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetPaymentsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<PaymentDto> RecordPaymentAsync(Guid invoiceId, RecordPaymentRequest request, Guid userId, CancellationToken ct = default);
    Task DeletePaymentAsync(Guid invoiceId, Guid paymentId, Guid userId, string userRole, CancellationToken ct = default);
}
