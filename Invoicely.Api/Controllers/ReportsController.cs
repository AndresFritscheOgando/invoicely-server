using Invoicely.Core.DTOs;
using Invoicely.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Invoicely.Api.Controllers;

/// <summary>Filterable invoice reports and CSV export. Requires Finance Manager or Admin.</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = "FinanceOrAdmin")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>Return summary metrics and invoice rows matching the given filters.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var result = await reportService.GetSummaryAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Export matching invoices as a UTF-8 CSV file download.</summary>
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var csv = await reportService.ExportCsvAsync(query, ct);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileName = $"invoices-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
