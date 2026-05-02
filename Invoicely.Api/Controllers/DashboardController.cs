using Invoicely.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoicely.Api.Controllers;

/// <summary>Aggregated dashboard statistics for the current user's workspace.</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "AllRoles")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>Return invoice counts, totals, status breakdown, monthly spend, and top vendors.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = await dashboardService.GetStatsAsync(ct);
        return Ok(stats);
    }
}
