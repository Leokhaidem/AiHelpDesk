using System.Threading.Channels;
using AIHelpDesk.Application.AgentAssignment.Services;
using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Application.Tickets.DTOs;
using AIHelpDesk.Application.Tickets.Services;
using AIHelpDesk.Application.Common;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Services;

/// <summary>
/// Full ticket lifecycle service. Enforces state machine transitions,
/// records history, and queues tickets for async AI analysis on creation.
/// </summary>
public sealed class TicketService : ITicketService
{
    private readonly AppDbContext _db;
    private readonly Channel<int> _aiChannel;
    private readonly ITicketHistoryRepository _history;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAgentAssignmentService _assignment;

    public TicketService(
        AppDbContext db,
        Channel<int> aiChannel,
        ITicketHistoryRepository history,
        UserManager<ApplicationUser> userManager,
        IAgentAssignmentService assignment)
    {
        _db = db;
        _aiChannel = aiChannel;
        _history = history;
        _userManager = userManager;
        _assignment = assignment;
    }

    // ── Employee ──────────────────────────────────────────────────────────────

    public async Task<TicketDetailResponse> CreateTicketAsync(
        string employeeId, CreateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = new Ticket
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            EmployeeId = employeeId,
            Status = TicketStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticket.Id,
            EventType = TicketEventType.Created,
            NewValue = TicketStatus.Created.ToString(),
            ActorId = employeeId,
            Timestamp = DateTime.UtcNow
        }, ct);

        // Queue for async AI analysis — fire and forget via channel
        await _aiChannel.Writer.WriteAsync(ticket.Id, ct);

        return await BuildDetailAsync(ticket, ct);
    }

    public async Task<PagedResult<TicketSummaryResponse>> GetMyTicketsAsync(
        string employeeId, TicketListQuery query, CancellationToken ct = default)
    {
        var q = _db.Tickets
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId);

        q = await ApplyFiltersAsync(q, query with { EmployeeId = null, AgentInactive = false }, ct);
        return await ToPagedResultAsync(q, query, ct);
    }

    public async Task<TicketDetailResponse?> GetMyTicketAsync(
        int ticketId, string employeeId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.EmployeeId == employeeId, ct);

        return ticket is null ? null : await BuildDetailAsync(ticket, ct);
    }

    // ── Agent ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<TicketSummaryResponse>> GetAssignedTicketsAsync(
        string agentId, TicketListQuery query, CancellationToken ct = default)
    {
        var q = _db.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedAgentId == agentId);

        q = await ApplyFiltersAsync(q, query with { AssignedAgentId = null, AgentInactive = false }, ct);
        return await ToPagedResultAsync(q, query, ct);
    }

    public async Task<TicketDetailResponse?> GetAssignedTicketAsync(
        int ticketId, string agentId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.AssignedAgentId == agentId, ct);

        return ticket is null ? null : await BuildDetailAsync(ticket, ct);
    }

    public async Task StartWorkAsync(
        int ticketId, string agentId, CancellationToken ct = default)
    {
        var ticket = await GetTicketForAgentAsync(ticketId, agentId, ct);
        var oldStatus = ticket.Status;

        TicketStateMachine.ValidateTransition(ticket.Status, TicketStatus.InProgress);

        ticket.Status = TicketStatus.InProgress;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.StatusChanged,
            OldValue = oldStatus.ToString(),
            NewValue = TicketStatus.InProgress.ToString(),
            ActorId = agentId,
            Timestamp = DateTime.UtcNow
        }, ct);
    }

    public async Task ResolveAsync(
        int ticketId, string agentId, ResolveTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await GetTicketForAgentAsync(ticketId, agentId, ct);
        var oldStatus = ticket.Status;

        TicketStateMachine.ValidateTransition(ticket.Status, TicketStatus.Resolved);

        ticket.Status = TicketStatus.Resolved;
        ticket.Resolution = request.Resolution.Trim();
        ticket.ResolvedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.Resolved,
            OldValue = oldStatus.ToString(),
            NewValue = TicketStatus.Resolved.ToString(),
            ActorId = agentId,
            Timestamp = DateTime.UtcNow
        }, ct);

        // The agent just freed capacity — let the sweep offer waiting tickets.
        await _assignment.SweepAsync(ct: ct);
    }

    public async Task CloseAsync(
        int ticketId, string actorId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FindAsync([ticketId], ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        var oldStatus = ticket.Status;
        TicketStateMachine.ValidateTransition(ticket.Status, TicketStatus.Closed);

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.Closed,
            OldValue = oldStatus.ToString(),
            NewValue = TicketStatus.Closed.ToString(),
            ActorId = actorId,
            Timestamp = DateTime.UtcNow
        }, ct);

        // The agent just freed capacity — let the sweep offer waiting tickets.
        await _assignment.SweepAsync(ct: ct);
    }

    public async Task ReleaseAsync(
        int ticketId, string agentId, CancellationToken ct = default)
    {
        var ticket = await GetTicketForAgentAsync(ticketId, agentId, ct);
        var oldStatus = ticket.Status;

        // Only unstarted tickets can be released (Assigned → AwaitingAssignment).
        TicketStateMachine.ValidateTransition(ticket.Status, TicketStatus.AwaitingAssignment);

        ticket.AssignedAgentId = null;
        ticket.Status = TicketStatus.AwaitingAssignment;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.StatusChanged,
            OldValue = oldStatus.ToString(),
            NewValue = TicketStatus.AwaitingAssignment.ToString(),
            ActorId = agentId,
            Timestamp = DateTime.UtcNow,
            Details = "Agent released the ticket before starting work."
        }, ct);

        // Immediately re-offer the ticket, but do not hand it back to the same agent.
        await _assignment.SweepAsync(excludeAgentId: agentId, ct: ct);
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<TicketSummaryResponse>> GetAllTicketsAsync(
        TicketListQuery query, CancellationToken ct = default)
    {
        var q = _db.Tickets.AsNoTracking();
        q = await ApplyFiltersAsync(q, query, ct);
        return await ToPagedResultAsync(q, query, ct);
    }

    public async Task<TicketDetailResponse?> GetTicketByIdAsync(
        int ticketId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        return ticket is null ? null : await BuildDetailAsync(ticket, ct);
    }

    public async Task AssignAgentAsync(
        int ticketId, AdminAssignAgentRequest request, string adminId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FindAsync([ticketId], ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        var agent = await _userManager.FindByIdAsync(request.AgentId)
            ?? throw new InvalidOperationException("Agent not found.");

        if (!await _userManager.IsInRoleAsync(agent, "Agent"))
            throw new InvalidOperationException("The specified user is not in the Agent role.");

        if (!agent.IsActive)
            throw new InvalidOperationException("The specified agent is deactivated.");

        // Tickets that are being worked on, resolved or closed cannot be (re)assigned.
        if (ticket.Status is not (TicketStatus.Created
            or TicketStatus.AnalysisInProgress
            or TicketStatus.AnalysisFailed
            or TicketStatus.AwaitingAssignment
            or TicketStatus.Assigned))
            throw new InvalidOperationException(
                $"A ticket in status '{ticket.Status}' cannot be assigned to an agent.");

        var oldAgentId = ticket.AssignedAgentId;
        ticket.AssignedAgentId = request.AgentId;

        // Move analysis/waiting states into Assigned; reassignment keeps the status.
        if (ticket.Status != TicketStatus.Assigned)
        {
            TicketStateMachine.ValidateTransition(ticket.Status, TicketStatus.Assigned);
            ticket.Status = TicketStatus.Assigned;
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.AgentAssigned,
            OldValue = oldAgentId,
            NewValue = request.AgentId,
            ActorId = adminId,
            Timestamp = DateTime.UtcNow,
            Details = "Manually assigned by admin."
        }, ct);
    }

    public async Task OverrideAnalysisAsync(
        int ticketId, AdminOverrideAnalysisRequest request, string adminId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FindAsync([ticketId], ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        if (ticket.Status != TicketStatus.AnalysisFailed)
            throw new InvalidOperationException("Analysis can only be overridden for tickets in AnalysisFailed status.");

        var category = await _db.Categories.FindAsync([request.CategoryId], ct)
            ?? throw new InvalidOperationException("Category not found.");
        var priority = await _db.Priorities.FindAsync([request.PriorityId], ct)
            ?? throw new InvalidOperationException("Priority not found.");

        ticket.CategoryId = category.Id;
        ticket.PriorityId = priority.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.AgentId))
        {
            var agent = await _userManager.FindByIdAsync(request.AgentId)
                ?? throw new InvalidOperationException("Agent not found.");
            if (!await _userManager.IsInRoleAsync(agent, "Agent"))
                throw new InvalidOperationException("The specified user is not in the Agent role.");
            ticket.AssignedAgentId = request.AgentId;
        }

        ticket.Status = TicketStatus.Assigned;
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.CategoryOverridden,
            OldValue = "AnalysisFailed",
            NewValue = $"Category: {category.Name}, Priority: {priority.Name}",
            ActorId = adminId,
            Timestamp = DateTime.UtcNow,
            Details = "Admin manually overrode AI analysis result."
        }, ct);
    }

    public async Task RetryAnalysisAsync(
        int ticketId, string adminId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FindAsync([ticketId], ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        if (ticket.Status != TicketStatus.AnalysisFailed)
            throw new InvalidOperationException("Only tickets with AnalysisFailed status can be retried.");

        ticket.Status = TicketStatus.Created;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.AiAnalysisRetried,
            OldValue = TicketStatus.AnalysisFailed.ToString(),
            NewValue = TicketStatus.Created.ToString(),
            ActorId = adminId,
            Timestamp = DateTime.UtcNow,
            Details = "Admin triggered AI analysis retry."
        }, ct);

        // Re-queue for AI analysis
        await _aiChannel.Writer.WriteAsync(ticket.Id, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Ticket> GetTicketForAgentAsync(
        int ticketId, string agentId, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.AssignedAgentId == agentId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found or not assigned to this agent.");
        return ticket;
    }

    private async Task<IQueryable<Ticket>> ApplyFiltersAsync(
        IQueryable<Ticket> q, TicketListQuery query, CancellationToken ct)
    {
        if (query.Status.HasValue) q = q.Where(t => t.Status == query.Status.Value);
        if (query.CategoryId.HasValue) q = q.Where(t => t.CategoryId == query.CategoryId);
        if (query.PriorityId.HasValue) q = q.Where(t => t.PriorityId == query.PriorityId);
        if (!string.IsNullOrEmpty(query.AssignedAgentId)) q = q.Where(t => t.AssignedAgentId == query.AssignedAgentId);
        if (!string.IsNullOrEmpty(query.EmployeeId)) q = q.Where(t => t.EmployeeId == query.EmployeeId);
        if (query.FromDate.HasValue) q = q.Where(t => t.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue) q = q.Where(t => t.CreatedAt <= query.ToDate.Value);

        if (query.AgentInactive)
        {
            // Open tickets still held by an agent who is no longer eligible:
            // the user is missing, deactivated or lost the Agent role.
            var agentRoleId = await _db.Roles
                .AsNoTracking()
                .Where(r => r.Name == "Agent")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            q = q.Where(t => t.AssignedAgentId != null
                && (t.Status == TicketStatus.Assigned || t.Status == TicketStatus.InProgress)
                && !(_db.Users.Any(u => u.Id == t.AssignedAgentId && u.IsActive)
                    && agentRoleId != null
                    && _db.UserRoles.Any(ur => ur.UserId == t.AssignedAgentId && ur.RoleId == agentRoleId)));
        }

        return q;
    }

    private async Task<PagedResult<TicketSummaryResponse>> ToPagedResultAsync(
        IQueryable<Ticket> q, TicketListQuery query, CancellationToken ct)
    {
        var totalCount = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .ToListAsync(ct);

        var summaries = new List<TicketSummaryResponse>();
        foreach (var t in items)
        {
            string? agentName = null;
            if (!string.IsNullOrEmpty(t.AssignedAgentId))
            {
                var agent = await _userManager.FindByIdAsync(t.AssignedAgentId);
                agentName = agent?.FullName;
            }
            summaries.Add(new TicketSummaryResponse
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                CategoryName = t.Category?.Name,
                PriorityName = t.Priority?.Name,
                AssignedAgentId = t.AssignedAgentId,
                AssignedAgentName = agentName,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            });
        }

        return new PagedResult<TicketSummaryResponse>
        {
            Items = summaries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task<TicketDetailResponse> BuildDetailAsync(Ticket t, CancellationToken ct)
    {
        string? employeeName = null;
        string? agentName = null;

        var employee = await _userManager.FindByIdAsync(t.EmployeeId);
        employeeName = employee?.FullName;

        if (!string.IsNullOrEmpty(t.AssignedAgentId))
        {
            var agent = await _userManager.FindByIdAsync(t.AssignedAgentId);
            agentName = agent?.FullName;
        }

        return new TicketDetailResponse
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status.ToString(),
            AiSummary = t.AiSummary,
            AiSuggestedResolution = t.AiSuggestedResolution,
            CategoryId = t.CategoryId,
            CategoryName = t.Category?.Name,
            PriorityId = t.PriorityId,
            PriorityName = t.Priority?.Name,
            EmployeeId = t.EmployeeId,
            EmployeeName = employeeName,
            AssignedAgentId = t.AssignedAgentId,
            AssignedAgentName = agentName,
            Resolution = t.Resolution,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            ResolvedAt = t.ResolvedAt,
            ClosedAt = t.ClosedAt
        };
    }
}
