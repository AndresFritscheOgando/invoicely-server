using Invoicely.Core.DTOs;

namespace Invoicely.Core.Interfaces;

public interface IVendorService
{
    Task<PaginatedResult<VendorDto>> GetVendorsAsync(VendorListQuery query, CancellationToken ct = default);
    Task<VendorDto> GetVendorByIdAsync(Guid id, CancellationToken ct = default);
    Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken ct = default);
    Task<VendorDto> UpdateVendorAsync(Guid id, UpdateVendorRequest request, CancellationToken ct = default);
    Task UpdateVendorStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task DeleteVendorAsync(Guid id, CancellationToken ct = default);
}
