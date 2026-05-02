using FluentValidation;
using Invoicely.Core.DTOs;
using Invoicely.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Invoicely.Api.Controllers;

/// <summary>Payment recording and deletion for approved invoices.</summary>
[ApiController]
[Route("api/invoices/{invoiceId:guid}/payments")]
[Authorize]
public class PaymentsController(
    IPaymentService paymentService,
    IValidator<RecordPaymentRequest> validator) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentUserRole =>
        User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<IActionResult> GetPayments(Guid invoiceId, CancellationToken ct)
    {
        try
        {
            var payments = await paymentService.GetPaymentsForInvoiceAsync(invoiceId, ct);
            return Ok(payments);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Policy = "FinanceOrAdmin")]
    public async Task<IActionResult> RecordPayment(Guid invoiceId, [FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var payment = await paymentService.RecordPaymentAsync(invoiceId, request, CurrentUserId, ct);
            return Ok(payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{paymentId:guid}")]
    [Authorize(Policy = "FinanceOrAdmin")]
    public async Task<IActionResult> DeletePayment(Guid invoiceId, Guid paymentId, CancellationToken ct)
    {
        try
        {
            await paymentService.DeletePaymentAsync(invoiceId, paymentId, CurrentUserId, CurrentUserRole, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
