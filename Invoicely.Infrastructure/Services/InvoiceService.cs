using Invoicely.Core.DTOs;
using Invoicely.Core.Entities;
using Invoicely.Core.Enums;
using Invoicely.Core.Interfaces;
using Invoicely.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Invoicely.Infrastructure.Services;

public class InvoiceService(AppDbContext db) : IInvoiceService
{
    public async Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesAsync(InvoiceListQuery query, CancellationToken ct = default)
    {
        var q = db.Invoices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            q = q.Where(i =>
                i.InvoiceNumber.ToLower().Contains(search) ||
                (i.Description != null && i.Description.ToLower().Contains(search)) ||
                i.Vendor.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<InvoiceStatus>(query.Status, out var status))
            q = q.Where(i => i.Status == status);

        if (query.VendorId.HasValue)
            q = q.Where(i => i.VendorId == query.VendorId.Value);

        if (!string.IsNullOrWhiteSpace(query.DateFrom) && DateOnly.TryParse(query.DateFrom, out var dateFrom))
            q = q.Where(i => i.IssueDate >= dateFrom);

        if (!string.IsNullOrWhiteSpace(query.DateTo) && DateOnly.TryParse(query.DateTo, out var dateTo))
            q = q.Where(i => i.IssueDate <= dateTo);

        var totalCount = await q.CountAsync(ct);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await q
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceSummaryDto(
                i.Id,
                i.InvoiceNumber,
                i.VendorId,
                i.Vendor.Name,
                i.CreatedBy.Name,
                i.Amount,
                i.Currency,
                i.IssueDate.ToString("yyyy-MM-dd"),
                i.DueDate.ToString("yyyy-MM-dd"),
                i.Status.ToString(),
                i.PaymentStatus.ToString(),
                i.Description,
                i.CreatedAt,
                i.UpdatedAt))
            .ToListAsync(ct);

        return new PaginatedResult<InvoiceSummaryDto>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Vendor)
            .Include(i => i.CreatedBy)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found.");

        return ToDto(invoice);
    }

    public async Task<IEnumerable<InvoiceCommentDto>> GetCommentsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var exists = await db.Invoices.AsNoTracking().AnyAsync(i => i.Id == invoiceId, ct);
        if (!exists)
            throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        var comments = await db.InvoiceComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.InvoiceId == invoiceId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return comments.Select(c => new InvoiceCommentDto(
            c.Id,
            c.InvoiceId,
            ToActivityUserDto(c.User),
            c.Content,
            c.CreatedAt));
    }

    public async Task<InvoiceCommentDto> AddCommentAsync(Guid invoiceId, Guid userId, string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content is required.");

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        var comment = new InvoiceComment
        {
            InvoiceId = invoice.Id,
            UserId = userId,
            Content = content.Trim(),
        };

        db.InvoiceComments.Add(comment);
        await AddAuditLogAsync(userId, invoice.Id, AuditAction.InvoiceCommentAdded, newValue: comment.Content, ct: ct);
        await db.SaveChangesAsync(ct);

        await db.Entry(comment).Reference(c => c.User).LoadAsync(ct);

        return new InvoiceCommentDto(
            comment.Id,
            comment.InvoiceId,
            ToActivityUserDto(comment.User),
            comment.Content,
            comment.CreatedAt);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var exists = await db.Invoices.AsNoTracking().AnyAsync(i => i.Id == invoiceId, ct);
        if (!exists)
            throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        var logs = await db.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.EntityType == nameof(Invoice) && a.EntityId == invoiceId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return logs.Select(a => new AuditLogDto(
            a.Id,
            ToActivityUserDto(a.User),
            a.EntityType,
            a.EntityId,
            a.Action.ToString(),
            a.OldValue,
            a.NewValue,
            a.CreatedAt));
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, Guid userId, CancellationToken ct = default)
    {
        var vendor = await db.Vendors.FindAsync([request.VendorId], ct)
            ?? throw new KeyNotFoundException($"Vendor {request.VendorId} not found.");

        if (vendor.Status != VendorStatus.Active)
            throw new InvalidOperationException("Cannot create invoice for an inactive or blocked vendor.");

        if (!DateOnly.TryParse(request.IssueDate, out var issueDate))
            throw new ArgumentException("Invalid issue date.");

        if (!DateOnly.TryParse(request.DueDate, out var dueDate))
            throw new ArgumentException("Invalid due date.");

        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var items = request.Items.Select(item => new InvoiceItem
        {
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Total = Math.Round(item.Quantity * item.UnitPrice, 2),
        }).ToList();

        var total = items.Sum(i => i.Total);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            VendorId = request.VendorId,
            CreatedByUserId = userId,
            Amount = total,
            Currency = request.Currency,
            IssueDate = issueDate,
            DueDate = dueDate,
            Description = request.Description,
            Items = items,
        };

        db.Invoices.Add(invoice);
        await AddAuditLogAsync(userId, invoice.Id, AuditAction.InvoiceCreated,
            newValue: $"Created {invoice.InvoiceNumber} with total {total:0.00} {invoice.Currency}.", ct: ct);
        await db.SaveChangesAsync(ct);

        await db.Entry(invoice).Reference(i => i.Vendor).LoadAsync(ct);
        await db.Entry(invoice).Reference(i => i.CreatedBy).LoadAsync(ct);

        return ToDto(invoice);
    }

    public async Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceRequest request, Guid userId, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Vendor)
            .Include(i => i.CreatedBy)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be edited.");

        var vendor = await db.Vendors.FindAsync([request.VendorId], ct)
            ?? throw new KeyNotFoundException($"Vendor {request.VendorId} not found.");

        if (vendor.Status != VendorStatus.Active)
            throw new InvalidOperationException("Cannot assign invoice to an inactive or blocked vendor.");

        if (!DateOnly.TryParse(request.IssueDate, out var issueDate))
            throw new ArgumentException("Invalid issue date.");

        if (!DateOnly.TryParse(request.DueDate, out var dueDate))
            throw new ArgumentException("Invalid due date.");

        var previousSummary = $"Vendor={invoice.Vendor?.Name ?? invoice.VendorId.ToString()}, Total={invoice.Amount:0.00} {invoice.Currency}, Due={invoice.DueDate:yyyy-MM-dd}";

        db.InvoiceItems.RemoveRange(invoice.Items);

        var items = request.Items.Select(item => new InvoiceItem
        {
            InvoiceId = invoice.Id,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Total = Math.Round(item.Quantity * item.UnitPrice, 2),
        }).ToList();

        invoice.VendorId = request.VendorId;
        invoice.IssueDate = issueDate;
        invoice.DueDate = dueDate;
        invoice.Description = request.Description;
        invoice.Currency = request.Currency;
        invoice.Items = items;
        invoice.Amount = items.Sum(i => i.Total);

        var newSummary = $"Vendor={vendor.Name}, Total={invoice.Amount:0.00} {invoice.Currency}, Due={invoice.DueDate:yyyy-MM-dd}";
        await AddAuditLogAsync(userId, invoice.Id, AuditAction.InvoiceUpdated, previousSummary, newSummary, ct);

        await db.SaveChangesAsync(ct);

        await db.Entry(invoice).Reference(i => i.Vendor).LoadAsync(ct);
        await db.Entry(invoice).Reference(i => i.CreatedBy).LoadAsync(ct);

        return ToDto(invoice);
    }

    public async Task DeleteInvoiceAsync(Guid id, Guid userId, string userRole, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be deleted.");

        var isOwner = invoice.CreatedByUserId == userId;
        var isPrivileged = userRole is "Admin" or "FinanceManager";

        if (!isOwner && !isPrivileged)
            throw new UnauthorizedAccessException("You do not have permission to delete this invoice.");

        await AddAuditLogAsync(userId, invoice.Id, AuditAction.InvoiceDeleted,
            oldValue: $"Deleted draft invoice {invoice.InvoiceNumber}.", ct: ct);
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
    }

    public async Task<InvoiceDto> SubmitInvoiceAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var invoice = await LoadFullInvoiceAsync(id, ct);

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be submitted.");

        if (invoice.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Only the invoice creator can submit it.");

        var previousStatus = invoice.Status.ToString();
        invoice.Status = InvoiceStatus.Submitted;
        await AddAuditLogAsync(userId, invoice.Id, AuditAction.InvoiceSubmitted, previousStatus, invoice.Status.ToString(), ct);
        await db.SaveChangesAsync(ct);

        return ToDto(invoice);
    }

    public async Task<InvoiceDto> ApproveInvoiceAsync(Guid id, Guid reviewerId, CancellationToken ct = default)
    {
        var invoice = await LoadFullInvoiceAsync(id, ct);

        if (invoice.Status != InvoiceStatus.Submitted)
            throw new InvalidOperationException("Only Submitted invoices can be approved.");

        var previousStatus = invoice.Status.ToString();
        invoice.Status = InvoiceStatus.Approved;
        invoice.ReviewedByUserId = reviewerId;
        invoice.ReviewedAt = DateTime.UtcNow;
        invoice.RejectionReason = null;

        await AddAuditLogAsync(reviewerId, invoice.Id, AuditAction.InvoiceApproved, previousStatus, invoice.Status.ToString(), ct);
        await db.SaveChangesAsync(ct);
        await db.Entry(invoice).Reference(i => i.ReviewedBy).LoadAsync(ct);

        return ToDto(invoice);
    }

    public async Task<InvoiceDto> RejectInvoiceAsync(Guid id, Guid reviewerId, string reason, CancellationToken ct = default)
    {
        var invoice = await LoadFullInvoiceAsync(id, ct);

        if (invoice.Status != InvoiceStatus.Submitted)
            throw new InvalidOperationException("Only Submitted invoices can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.");

        var previousStatus = invoice.Status.ToString();
        invoice.Status = InvoiceStatus.Rejected;
        invoice.ReviewedByUserId = reviewerId;
        invoice.ReviewedAt = DateTime.UtcNow;
        invoice.RejectionReason = reason;

        await AddAuditLogAsync(reviewerId, invoice.Id, AuditAction.InvoiceRejected, previousStatus, reason.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await db.Entry(invoice).Reference(i => i.ReviewedBy).LoadAsync(ct);

        return ToDto(invoice);
    }

    public async Task<InvoiceDto> CancelInvoiceAsync(Guid id, Guid userId, string userRole, CancellationToken ct = default)
    {
        var invoice = await LoadFullInvoiceAsync(id, ct);

        if (invoice.Status == InvoiceStatus.Approved || invoice.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel an invoice with status {invoice.Status}.");

        var isOwner = invoice.CreatedByUserId == userId;
        var isPrivileged = userRole is "Admin" or "FinanceManager";

        if (!isOwner && !isPrivileged)
            throw new UnauthorizedAccessException("You do not have permission to cancel this invoice.");

        var previousStatus = invoice.Status.ToString();
        invoice.Status = InvoiceStatus.Cancelled;
        await AddAuditLogAsync(userId, invoice.Id, AuditAction.InvoiceCancelled, previousStatus, invoice.Status.ToString(), ct);
        await db.SaveChangesAsync(ct);

        return ToDto(invoice);
    }

    private async Task<Invoice> LoadFullInvoiceAsync(Guid id, CancellationToken ct)
    {
        return await db.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.CreatedBy)
            .Include(i => i.Items)
            .Include(i => i.ReviewedBy)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found.");
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";

        var lastNumber = await db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(ct);

        var sequence = 1;
        if (lastNumber is not null)
        {
            var suffix = lastNumber[prefix.Length..];
            if (int.TryParse(suffix, out var last))
                sequence = last + 1;
        }

        return $"{prefix}{sequence:D4}";
    }

    private Task AddAuditLogAsync(
        Guid userId,
        Guid entityId,
        AuditAction action,
        string? oldValue = null,
        string? newValue = null,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            EntityType = nameof(Invoice),
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
        });

        return Task.CompletedTask;
    }

    private static InvoiceDto ToDto(Invoice i) => new(
        i.Id,
        i.InvoiceNumber,
        i.VendorId,
        i.Vendor?.Name ?? string.Empty,
        i.CreatedByUserId,
        i.CreatedBy?.Name ?? string.Empty,
        i.Amount,
        i.Currency,
        i.IssueDate.ToString("yyyy-MM-dd"),
        i.DueDate.ToString("yyyy-MM-dd"),
        i.Status.ToString(),
        i.PaymentStatus.ToString(),
        i.Description,
        i.FileUrl,
        i.Items.Select(item => new InvoiceItemDto(item.Id, item.Description, item.Quantity, item.UnitPrice, item.Total)),
        i.CreatedAt,
        i.UpdatedAt,
        i.ReviewedByUserId,
        i.ReviewedBy?.Name,
        i.ReviewedAt,
        i.RejectionReason);

    private static ActivityUserDto ToActivityUserDto(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role.ToString());
}
