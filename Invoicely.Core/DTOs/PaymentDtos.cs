using Invoicely.Core.Enums;

namespace Invoicely.Core.DTOs;

public record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string PaymentDate,
    string PaymentMethod,
    string? ReferenceNumber,
    string CreatedByName,
    DateTime CreatedAt);

public record RecordPaymentRequest(
    decimal Amount,
    string PaymentDate,
    string PaymentMethod,
    string? ReferenceNumber);
