using AIHelpDesk.Application.Dashboard.DTOs;
using AIHelpDesk.Application.Tickets.DTOs;

namespace AIHelpDesk.Application.Dashboard.Services;

public interface IDashboardService
{
    /// <summary>Returns dashboard metrics. Agents only see their assigned tickets.</summary>
    Task<DashboardMetrics> GetMetricsAsync(string? agentId = null, CancellationToken ct = default);
}

public interface IReportService
{
    Task<PagedResult<TicketSummaryResponse>> GetReportAsync(ReportQuery query, string? agentId = null, CancellationToken ct = default);
}
