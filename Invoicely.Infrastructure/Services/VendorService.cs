using Invoicely.Core.DTOs;
using Invoicely.Core.Entities;
using Invoicely.Core.Enums;
using Invoicely.Core.Interfaces;
using Invoicely.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Invoicely.Infrastructure.Services;

public class VendorService(AppDbContext db) : IVendorService
{
    public async Task<PaginatedResult<VendorDto>> GetVendorsAsync(VendorListQuery query, CancellationToken ct = default)
    {
        var q = db.Vendors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            q = q.Where(v => v.Name.ToLower().Contains(search) || v.Email.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VendorStatus>(query.Status, out var status))
            q = q.Where(v => v.Status == status);

        var totalCount = await q.CountAsync(ct);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var vendors = await q
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new
            {
                Vendor = v,
                InvoiceCount = v.Invoices.Count
            })
            .ToListAsync(ct);

        var items = vendors.Select(x => ToDto(x.Vendor, x.InvoiceCount));

        return new PaginatedResult<VendorDto>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<VendorDto> GetVendorByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await db.Vendors
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new { Vendor = v, InvoiceCount = v.Invoices.Count })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Vendor {id} not found.");

        return ToDto(result.Vendor, result.InvoiceCount);
    }

    public async Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken ct = default)
    {
        if (await db.Vendors.AnyAsync(v => v.Email == request.Email, ct))
            throw new InvalidOperationException("A vendor with this email already exists.");

        var vendor = new Vendor
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            TaxId = request.TaxId,
            Address = request.Address,
        };

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(ct);

        return ToDto(vendor, 0);
    }

    public async Task<VendorDto> UpdateVendorAsync(Guid id, UpdateVendorRequest request, CancellationToken ct = default)
    {
        var vendor = await db.Vendors.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Vendor {id} not found.");

        if (await db.Vendors.AnyAsync(v => v.Email == request.Email && v.Id != id, ct))
            throw new InvalidOperationException("A vendor with this email already exists.");

        vendor.Name = request.Name;
        vendor.Email = request.Email;
        vendor.Phone = request.Phone;
        vendor.TaxId = request.TaxId;
        vendor.Address = request.Address;

        await db.SaveChangesAsync(ct);

        var invoiceCount = await db.Invoices.CountAsync(i => i.VendorId == id, ct);
        return ToDto(vendor, invoiceCount);
    }

    public async Task UpdateVendorStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        if (!Enum.TryParse<VendorStatus>(status, out var vendorStatus))
            throw new ArgumentException($"Invalid status: {status}");

        var vendor = await db.Vendors.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Vendor {id} not found.");

        vendor.Status = vendorStatus;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteVendorAsync(Guid id, CancellationToken ct = default)
    {
        var vendor = await db.Vendors.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Vendor {id} not found.");

        var hasInvoices = await db.Invoices.AnyAsync(i => i.VendorId == id, ct);
        if (hasInvoices)
            throw new InvalidOperationException("Cannot delete a vendor with existing invoices.");

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync(ct);
    }

    private static VendorDto ToDto(Vendor v, int invoiceCount) =>
        new(v.Id, v.Name, v.Email, v.Phone, v.TaxId, v.Address, v.Status.ToString(), invoiceCount, v.CreatedAt, v.UpdatedAt);
}
