namespace Invoicely.Core.Enums;

public enum AuditAction
{
    InvoiceCreated,
    InvoiceSubmitted,
    InvoiceApproved,
    InvoiceRejected,
    InvoicePaid,
    InvoiceCancelled,
    InvoiceUpdated,
    InvoiceDeleted,
    InvoiceCommentAdded,
    VendorCreated,
    VendorUpdated,
    VendorDeleted,
    PaymentAdded,
    PaymentDeleted,
    UserCreated,
    UserUpdated
}
