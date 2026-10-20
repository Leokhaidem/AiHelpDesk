using System.Security.Claims;
using AIHelpDesk.Application.Dashboard.DTOs;
using AIHelpDesk.Application.Dashboard.Services;
using AIHelpDesk.Application.Tickets.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Dashboard and reporting endpoints.
/// Admins see all data; Agents see only their assigned scope.
/// </summary>
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly IReportService _reports;

    public DashboardController(IDashboardService dashboard, IReportService reports)
    {
        _dashboard = dashboard;
        _reports = reports;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsAgent => User.IsInRole("Agent");

    /// <summary>
    /// Get dashboard metrics.
    /// Admins see system-wide totals; Agents see only their assigned tickets.
    /// </summary>
    [HttpGet("api/dashboard")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        var agentScope = IsAdmin ? null : UserId;
        var metrics = await _dashboard.GetMetricsAsync(agentScope, ct);
        return Ok(metrics);
    }

    /// <summary>
    /// Get a filtered, paginated report.
    /// Admins can filter by any agent; Agents see only their tickets.
    /// </summary>
    [HttpGet("api/reports")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> GetReport([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var agentScope = IsAdmin ? null : UserId;
        var result = await _reports.GetReportAsync(query, agentScope, ct);
        return Ok(result);
    }
}
