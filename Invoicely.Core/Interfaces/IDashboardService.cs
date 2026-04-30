using Invoicely.Core.DTOs;

namespace Invoicely.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);
}
