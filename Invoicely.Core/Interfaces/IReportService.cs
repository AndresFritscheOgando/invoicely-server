using Invoicely.Core.DTOs;

namespace Invoicely.Core.Interfaces;

public interface IReportService
{
    Task<ReportSummaryDto> GetSummaryAsync(ReportQuery query, CancellationToken ct = default);
    Task<string> ExportCsvAsync(ReportQuery query, CancellationToken ct = default);
}
