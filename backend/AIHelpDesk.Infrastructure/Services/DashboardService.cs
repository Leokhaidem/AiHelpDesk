using AIHelpDesk.Application.Dashboard.DTOs;
using AIHelpDesk.Application.Dashboard.Services;
using AIHelpDesk.Application.Tickets.DTOs;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardMetrics> GetMetricsAsync(
        string? agentId = null, CancellationToken ct = default)
    {
        var q = _db.Tickets.AsNoTracking();
        if (!string.IsNullOrEmpty(agentId))
            q = q.Where(t => t.AssignedAgentId == agentId);

        var tickets = await q
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .ToListAsync(ct);

        return new DashboardMetrics
        {
            TotalTickets = tickets.Count,
            Created = tickets.Count(t => t.Status == TicketStatus.Created),
            AnalysisInProgress = tickets.Count(t => t.Status == TicketStatus.AnalysisInProgress),
            AnalysisFailed = tickets.Count(t => t.Status == TicketStatus.AnalysisFailed),
            AwaitingAssignment = tickets.Count(t => t.Status == TicketStatus.AwaitingAssignment),
            Assigned = tickets.Count(t => t.Status == TicketStatus.Assigned),
            InProgress = tickets.Count(t => t.Status == TicketStatus.InProgress),
            Resolved = tickets.Count(t => t.Status == TicketStatus.Resolved),
            Closed = tickets.Count(t => t.Status == TicketStatus.Closed),
            Unassigned = tickets.Count(t => t.AssignedAgentId == null),
            ByCategory = tickets
                .Where(t => t.Category is not null)
                .GroupBy(t => t.Category!.Name)
                .Select(g => new CategoryCount { CategoryName = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .ToList(),
            ByPriority = tickets
                .Where(t => t.Priority is not null)
                .GroupBy(t => t.Priority!.Name)
                .Select(g => new PriorityCount { PriorityName = g.Key, Count = g.Count() })
                .OrderByDescending(p => p.Count)
                .ToList()
        };
    }
}

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<PagedResult<TicketSummaryResponse>> GetReportAsync(
        ReportQuery query, string? agentId = null, CancellationToken ct = default)
    {
        var q = _db.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Priority);

        IQueryable<Domain.Entities.Ticket> filtered = q;

        if (!string.IsNullOrEmpty(agentId))
            filtered = filtered.Where(t => t.AssignedAgentId == agentId);

        if (!string.IsNullOrEmpty(query.Status) &&
            Enum.TryParse<TicketStatus>(query.Status, true, out var status))
            filtered = filtered.Where(t => t.Status == status);

        if (query.CategoryId.HasValue) filtered = filtered.Where(t => t.CategoryId == query.CategoryId);
        if (query.PriorityId.HasValue) filtered = filtered.Where(t => t.PriorityId == query.PriorityId);
        if (!string.IsNullOrEmpty(query.AgentId)) filtered = filtered.Where(t => t.AssignedAgentId == query.AgentId);
        if (query.FromDate.HasValue) filtered = filtered.Where(t => t.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue) filtered = filtered.Where(t => t.CreatedAt <= query.ToDate.Value);

        var total = await filtered.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await filtered
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TicketSummaryResponse>
        {
            Items = items.Select(t => new TicketSummaryResponse
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                CategoryName = t.Category?.Name,
                PriorityName = t.Priority?.Name,
                AssignedAgentId = t.AssignedAgentId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
