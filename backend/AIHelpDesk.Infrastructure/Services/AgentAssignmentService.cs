using AIHelpDesk.Application.AgentAssignment.Services;
using AIHelpDesk.Application.Common;
using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIHelpDesk.Infrastructure.Services;

/// <summary>
/// Flow F — picks an eligible support agent for a ticket and provides the
/// assignment sweep used whenever agent eligibility or capacity changes.
/// </summary>
public sealed class AgentAssignmentService : IAgentAssignmentService
{
    private const string AgentRoleName = "Agent";

    private readonly AppDbContext _db;
    private readonly ITicketHistoryRepository _history;
    private readonly ILogger<AgentAssignmentService> _logger;

    public AgentAssignmentService(
        AppDbContext db,
        ITicketHistoryRepository history,
        ILogger<AgentAssignmentService> logger)
    {
        _db = db;
        _history = history;
        _logger = logger;
    }

    public async Task<string?> TryAssignTicketAsync(
        int ticketId, string? excludeAgentId = null, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        // Only tickets that are analyzed (and being finalized) or waiting for an agent.
        if (ticket.Status is not (TicketStatus.AnalysisInProgress or TicketStatus.AwaitingAssignment))
            return null;

        if (ticket.CategoryId is null)
            return null;

        var categoryId = ticket.CategoryId.Value;

        // 1. Agents linked to the ticket's category.
        var candidates = await _db.AgentCategories
            .AsNoTracking()
            .Where(ac => ac.CategoryId == categoryId)
            .Select(ac => ac.AgentId)
            .Distinct()
            .ToListAsync(ct);

        if (!string.IsNullOrEmpty(excludeAgentId))
            candidates.RemoveAll(c => c == excludeAgentId);

        if (candidates.Count == 0)
            return null;

        // 2. Active users in the Agent role (through the AspNetUserRoles join).
        var agentRoleId = await _db.Roles
            .AsNoTracking()
            .Where(r => r.Name == AgentRoleName)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (agentRoleId is null)
            return null;

        var agentIds = await _db.Users
            .AsNoTracking()
            .Where(u => candidates.Contains(u.Id)
                && u.IsActive
                && _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == agentRoleId))
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (agentIds.Count == 0)
            return null;

        // 3. Agents whose profile says "available".
        var profiles = await _db.AgentProfiles
            .AsNoTracking()
            .Where(p => agentIds.Contains(p.UserId) && p.IsAvailable)
            .ToListAsync(ct);

        if (profiles.Count == 0)
            return null;

        // 4. Current workload per agent (Assigned + InProgress) and last activity.
        var openTickets = await _db.Tickets
            .AsNoTracking()
            .Where(t => agentIds.Contains(t.AssignedAgentId)
                && (t.Status == TicketStatus.Assigned || t.Status == TicketStatus.InProgress))
            .Select(t => new { t.AssignedAgentId, t.UpdatedAt, t.CreatedAt })
            .ToListAsync(ct);

        var openCounts = openTickets
            .GroupBy(t => t.AssignedAgentId!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Proxy for "when was this agent last assigned a ticket": the most recent
        // of their open tickets. Agents with no open tickets count as fully idle.
        var lastActivity = openTickets
            .GroupBy(t => t.AssignedAgentId!)
            .ToDictionary(g => g.Key, g => g.Max(t => t.UpdatedAt ?? t.CreatedAt));

        // 5. Under capacity; fewest open first; then longest idle; then stable order.
        var chosen = profiles
            .Where(p => !openCounts.TryGetValue(p.UserId, out var count) || count < p.MaxOpenTickets)
            .OrderBy(p => openCounts.GetValueOrDefault(p.UserId, 0))
            .ThenBy(p => lastActivity.GetValueOrDefault(p.UserId, DateTime.MinValue))
            .ThenBy(p => p.UserId)
            .FirstOrDefault();

        if (chosen is null)
            return null;

        TicketStateMachine.ValidateTransition(ticket.Status, TicketStatus.Assigned);

        var oldStatus = ticket.Status;
        ticket.AssignedAgentId = chosen.UserId;
        ticket.Status = TicketStatus.Assigned;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _history.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.AgentAssigned,
            OldValue = oldStatus.ToString(),
            NewValue = chosen.UserId,
            ActorId = "system",
            Timestamp = DateTime.UtcNow,
            Details = $"Auto-assigned by the assignment algorithm " +
                      $"(open tickets: {openCounts.GetValueOrDefault(chosen.UserId, 0)}/{chosen.MaxOpenTickets})."
        }, ct);

        _logger.LogInformation(
            "Ticket {TicketId} assigned to agent {AgentId} (category {CategoryId}).",
            ticketId, chosen.UserId, categoryId);

        return chosen.UserId;
    }

    public async Task<int> SweepAsync(
        int? categoryId = null, string? excludeAgentId = null, CancellationToken ct = default)
    {
        var waiting = await _db.Tickets
            .Where(t => t.Status == TicketStatus.AwaitingAssignment)
            .Where(t => categoryId == null || t.CategoryId == categoryId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var assigned = 0;
        foreach (var ticketId in waiting)
        {
            var agentId = await TryAssignTicketAsync(ticketId, excludeAgentId, ct);
            if (agentId is not null)
                assigned++;
        }

        if (assigned > 0)
        {
            _logger.LogInformation(
                "Assignment sweep assigned {Count} waiting ticket(s) (category {CategoryId}).",
                assigned, categoryId?.ToString() ?? "all");
        }

        return assigned;
    }
}