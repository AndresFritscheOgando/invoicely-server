using FluentValidation;
using Invoicely.Core.DTOs;
using Invoicely.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoicely.Api.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public class VendorsController(
    IVendorService vendorService,
    IValidator<CreateVendorRequest> createValidator,
    IValidator<UpdateVendorRequest> updateValidator,
    IValidator<UpdateVendorStatusRequest> statusValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<IActionResult> GetVendors([FromQuery] VendorListQuery query, CancellationToken ct)
    {
        var result = await vendorService.GetVendorsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AllRoles")]
    public async Task<IActionResult> GetVendor(Guid id, CancellationToken ct)
    {
        try
        {
            var vendor = await vendorService.GetVendorByIdAsync(id, ct);
            return Ok(vendor);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Policy = "FinanceOrAdmin")]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var vendor = await vendorService.CreateVendorAsync(request, ct);
            return CreatedAtAction(nameof(GetVendor), new { id = vendor.Id }, vendor);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "FinanceOrAdmin")]
    public async Task<IActionResult> UpdateVendor(Guid id, [FromBody] UpdateVendorRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var vendor = await vendorService.UpdateVendorAsync(id, request, ct);
            return Ok(vendor);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateVendorStatus(Guid id, [FromBody] UpdateVendorStatusRequest request, CancellationToken ct)
    {
        var validation = await statusValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            await vendorService.UpdateVendorStatusAsync(id, request.Status, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteVendor(Guid id, CancellationToken ct)
    {
        try
        {
            await vendorService.DeleteVendorAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
