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
    VendorCreated,
    VendorUpdated,
    VendorDeleted,
    PaymentAdded,
    UserCreated,
    UserUpdated
}
