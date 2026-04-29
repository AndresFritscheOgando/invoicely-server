using Invoicely.Core.DTOs;
using Invoicely.Core.Entities;
using Invoicely.Core.Enums;
using Invoicely.Core.Interfaces;
using Invoicely.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Invoicely.Infrastructure.Services;

public class PaymentService(AppDbContext db) : IPaymentService
{
    public async Task<IEnumerable<PaymentDto>> GetPaymentsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var exists = await db.Invoices.AnyAsync(i => i.Id == invoiceId, ct);
        if (!exists)
            throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        return await db.Payments
            .AsNoTracking()
            .Include(p => p.CreatedBy)
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id,
                p.InvoiceId,
                p.Amount,
                p.PaymentDate.ToString("yyyy-MM-dd"),
                p.PaymentMethod.ToString(),
                p.ReferenceNumber,
                p.CreatedBy.Name,
                p.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<PaymentDto> RecordPaymentAsync(Guid invoiceId, RecordPaymentRequest request, Guid userId, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Approved)
            throw new InvalidOperationException("Payments can only be recorded for Approved invoices.");

        if (!DateOnly.TryParse(request.PaymentDate, out var paymentDate))
            throw new ArgumentException("Invalid payment date.");

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, out var method))
            throw new ArgumentException($"Invalid payment method: {request.PaymentMethod}.");

        var totalPaid = invoice.Payments.Sum(p => p.Amount);
        var remaining = invoice.Amount - totalPaid;

        if (request.Amount > remaining)
            throw new InvalidOperationException($"Payment amount {request.Amount} exceeds remaining balance {remaining}.");

        var payment = new Payment
        {
            InvoiceId = invoiceId,
            Amount = request.Amount,
            PaymentDate = paymentDate,
            PaymentMethod = method,
            ReferenceNumber = request.ReferenceNumber,
            CreatedByUserId = userId,
        };

        db.Payments.Add(payment);

        totalPaid += request.Amount;
        invoice.PaymentStatus = totalPaid >= invoice.Amount
            ? PaymentStatus.Paid
            : PaymentStatus.PartiallyPaid;

        await db.SaveChangesAsync(ct);
        await db.Entry(payment).Reference(p => p.CreatedBy).LoadAsync(ct);

        return new PaymentDto(
            payment.Id,
            payment.InvoiceId,
            payment.Amount,
            payment.PaymentDate.ToString("yyyy-MM-dd"),
            payment.PaymentMethod.ToString(),
            payment.ReferenceNumber,
            payment.CreatedBy.Name,
            payment.CreatedAt);
    }

    public async Task DeletePaymentAsync(Guid invoiceId, Guid paymentId, Guid userId, string userRole, CancellationToken ct = default)
    {
        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.InvoiceId == invoiceId, ct)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        var isPrivileged = userRole is "Admin" or "FinanceManager";
        if (!isPrivileged && payment.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to delete this payment.");

        var invoice = await db.Invoices
            .Include(i => i.Payments)
            .FirstAsync(i => i.Id == invoiceId, ct);

        db.Payments.Remove(payment);

        var remainingTotal = invoice.Payments
            .Where(p => p.Id != paymentId)
            .Sum(p => p.Amount);

        invoice.PaymentStatus = remainingTotal <= 0
            ? PaymentStatus.Unpaid
            : remainingTotal >= invoice.Amount
                ? PaymentStatus.Paid
                : PaymentStatus.PartiallyPaid;

        await db.SaveChangesAsync(ct);
    }
}
