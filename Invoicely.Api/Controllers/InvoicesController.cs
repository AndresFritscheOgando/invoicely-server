using FluentValidation;
using Invoicely.Core.DTOs;
using Invoicely.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Invoicely.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController(
    IInvoiceService invoiceService,
    IValidator<CreateInvoiceRequest> createValidator,
    IValidator<UpdateInvoiceRequest> updateValidator) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentUserRole =>
        User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<IActionResult> GetInvoices([FromQuery] InvoiceListQuery query, CancellationToken ct)
    {
        var result = await invoiceService.GetInvoicesAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AllRoles")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
    {
        try
        {
            var invoice = await invoiceService.GetInvoiceByIdAsync(id, ct);
            return Ok(invoice);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateInvoice")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var invoice = await invoiceService.CreateInvoiceAsync(request, CurrentUserId, ct);
            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanCreateInvoice")]
    public async Task<IActionResult> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var invoice = await invoiceService.UpdateInvoiceAsync(id, request, CurrentUserId, ct);
            return Ok(invoice);
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

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = "CanCreateInvoice")]
    public async Task<IActionResult> SubmitInvoice(Guid id, CancellationToken ct)
    {
        try
        {
            var invoice = await invoiceService.SubmitInvoiceAsync(id, CurrentUserId, ct);
            return Ok(invoice);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "FinanceOrAdmin")]
    public async Task<IActionResult> ApproveInvoice(Guid id, CancellationToken ct)
    {
        try
        {
            var invoice = await invoiceService.ApproveInvoiceAsync(id, CurrentUserId, ct);
            return Ok(invoice);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "FinanceOrAdmin")]
    public async Task<IActionResult> RejectInvoice(Guid id, [FromBody] ApprovalActionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RejectionReason))
            return BadRequest(new { error = "Rejection reason is required." });

        try
        {
            var invoice = await invoiceService.RejectInvoiceAsync(id, CurrentUserId, request.RejectionReason, ct);
            return Ok(invoice);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "CanCreateInvoice")]
    public async Task<IActionResult> CancelInvoice(Guid id, CancellationToken ct)
    {
        try
        {
            var invoice = await invoiceService.CancelInvoiceAsync(id, CurrentUserId, CurrentUserRole, ct);
            return Ok(invoice);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanCreateInvoice")]
    public async Task<IActionResult> DeleteInvoice(Guid id, CancellationToken ct)
    {
        try
        {
            await invoiceService.DeleteInvoiceAsync(id, CurrentUserId, CurrentUserRole, ct);
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
