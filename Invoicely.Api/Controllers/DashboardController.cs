using Invoicely.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoicely.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "AllRoles")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = await dashboardService.GetStatsAsync(ct);
        return Ok(stats);
    }
}
