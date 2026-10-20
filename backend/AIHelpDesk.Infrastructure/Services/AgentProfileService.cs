using AIHelpDesk.Application.AgentApplications.DTOs;
using AIHelpDesk.Application.AgentAssignment.Services;
using AIHelpDesk.Application.AgentProfile.DTOs;
using AIHelpDesk.Application.AgentProfile.Services;
using AIHelpDesk.Application.Common;
using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIHelpDesk.Infrastructure.Services;

/// <summary>
/// Manages the AgentProfile (availability + capacity) and the AgentCategory links.
/// Runs the Flow I cleanup when an agent is deactivated or demoted.
/// Every change that can make tickets (un)assignable ends with an assignment sweep.
/// </summary>
public sealed class AgentProfileService : IAgentProfileService
{
    private const int DefaultMaxOpenTickets = 10;

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAgentAssignmentService _assignment;
    private readonly ITicketHistoryRepository _history;
    private readonly ILogger<AgentProfileService> _logger;

    public AgentProfileService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IAgentAssignmentService assignment,
        ITicketHistoryRepository history,
        ILogger<AgentProfileService> logger)
    {
        _db = db;
        _userManager = userManager;
        _assignment = assignment;
        _history = history;
        _logger = logger;
    }

    public async Task<AgentProfileResponse> GetMyProfileAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var profile = await _db.AgentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var openTicketCount = await _db.Tickets
            .CountAsync(t => t.AssignedAgentId == userId
                && (t.Status == TicketStatus.Assigned || t.Status == TicketStatus.InProgress), ct);

        return new AgentProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsAvailable = profile?.IsAvailable ?? true,
            MaxOpenTickets = profile?.MaxOpenTickets ?? DefaultMaxOpenTickets,
            OpenTicketCount = openTicketCount,
            Categories = await GetCategoriesAsync(userId, ct)
        };
    }

    public async Task<IReadOnlyList<CategoryRefResponse>> GetCategoriesAsync(string userId, CancellationToken ct = default)
    {
        var links = await _db.AgentCategories
            .AsNoTracking()
            .Include(ac => ac.Category)
            .Where(ac => ac.AgentId == userId)
            .ToListAsync(ct);

        return links
            .Where(ac => ac.Category is not null)
            .Select(ac => new CategoryRefResponse
            {
                Id = ac.Category!.Id,
                Name = ac.Category!.Name
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<CategoryRefResponse>> SetCategoriesAsync(
        string userId, IEnumerable<int> categoryIds, CancellationToken ct = default)
    {
        await EnsureAgentRoleAsync(userId);

        var ids = await ValidateCategoryIdsAsync(categoryIds);
        var idSet = ids.ToHashSet();

        var links = await _db.AgentCategories
            .Where(ac => ac.AgentId == userId)
            .ToListAsync(ct);
        var existingIds = links.Select(link => link.CategoryId).ToHashSet();

        // Replace by diff: re-adding a key that is still tracked (Deleted)
        // would make EF throw an identity-map conflict.
        _db.AgentCategories.RemoveRange(
            links.Where(link => !idSet.Contains(link.CategoryId)).ToList());

        foreach (var id in ids)
        {
            if (!existingIds.Contains(id))
                _db.AgentCategories.Add(new AgentCategory { AgentId = userId, CategoryId = id });
        }

        await _db.SaveChangesAsync(ct);

        // The category set changed — re-offer the waiting tickets.
        await _assignment.SweepAsync(ct: ct);

        return await GetCategoriesAsync(userId, ct);
    }

    public async Task<AgentProfileResponse> SetAvailabilityAsync(
        string userId, SetAgentAvailabilityRequest request, CancellationToken ct = default)
    {
        await EnsureAgentRoleAsync(userId);

        if (request.MaxOpenTickets is <= 0)
            throw new ArgumentException("MaxOpenTickets must be a positive number.");

        var profile = await _db.AgentProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile is null)
        {
            profile = new AgentProfile { UserId = userId };
            _db.AgentProfiles.Add(profile);
        }

        profile.IsAvailable = request.IsAvailable;

        if (request.MaxOpenTickets.HasValue)
            profile.MaxOpenTickets = request.MaxOpenTickets.Value;

        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Availability changed — re-offer the waiting tickets.
        await _assignment.SweepAsync(ct: ct);

        return await GetMyProfileAsync(userId, ct);
    }

    public async Task<IReadOnlyList<CategoryRefResponse>> EnsureAgentAsync(
        string userId, IEnumerable<int>? categoryIds, CancellationToken ct = default)
    {
        var profile = await _db.AgentProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var profileCreated = profile is null;

        if (profile is null)
        {
            profile = new AgentProfile { UserId = userId };
            _db.AgentProfiles.Add(profile);
        }

        if (categoryIds is not null)
        {
            var ids = await ValidateCategoryIdsAsync(categoryIds);
            var idSet = ids.ToHashSet();

            var links = await _db.AgentCategories
                .Where(ac => ac.AgentId == userId)
                .ToListAsync(ct);
            var existingIds = links.Select(link => link.CategoryId).ToHashSet();

            _db.AgentCategories.RemoveRange(
                links.Where(link => !idSet.Contains(link.CategoryId)).ToList());

            foreach (var id in ids)
            {
                if (!existingIds.Contains(id))
                    _db.AgentCategories.Add(new AgentCategory { AgentId = userId, CategoryId = id });
            }
        }

        await _db.SaveChangesAsync(ct);

        if (categoryIds is not null || profileCreated)
            await _assignment.SweepAsync(ct: ct);

        return await GetCategoriesAsync(userId, ct);
    }

    public async Task<AgentDeactivationResult> DeactivateAgentAsync(
        string userId, string adminId, CancellationToken ct = default)
    {
        // 1) The agent stops working: drop the category links and flip the profile.
        //    The profile row is kept so a reactivation does not lose the settings.
        var links = await _db.AgentCategories
            .Where(ac => ac.AgentId == userId)
            .ToListAsync(ct);

        _db.AgentCategories.RemoveRange(links);

        var profile = await _db.AgentProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile is not null)
        {
            profile.IsAvailable = false;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        // 2) Unstarted tickets go back to the queue; started ones stay with the agent
        //    and are flagged for manual review (GET /admin/tickets?agentInactive=true).
        var assignedTickets = await _db.Tickets
            .Where(t => t.AssignedAgentId == userId && t.Status == TicketStatus.Assigned)
            .ToListAsync(ct);

        foreach (var ticket in assignedTickets)
        {
            TicketStateMachine.ValidateTransition(ticket.Status, TicketStatus.AwaitingAssignment);

            ticket.AssignedAgentId = null;
            ticket.Status = TicketStatus.AwaitingAssignment;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _history.AddAsync(new TicketHistoryEvent
            {
                TicketId = ticket.Id,
                EventType = TicketEventType.StatusChanged,
                OldValue = TicketStatus.Assigned.ToString(),
                NewValue = TicketStatus.AwaitingAssignment.ToString(),
                ActorId = adminId,
                Timestamp = DateTime.UtcNow,
                Details = "The agent was deactivated or demoted; the ticket was requeued."
            }, ct);
        }

        var flagged = await _db.Tickets
            .CountAsync(t => t.AssignedAgentId == userId && t.Status == TicketStatus.InProgress, ct);

        await _db.SaveChangesAsync(ct);

        // 3) The freed capacity may let other agents pick up the requeued tickets.
        var autoAssigned = await _assignment.SweepAsync(ct: ct);

        _logger.LogInformation(
            "Agent {AgentId} deactivated: {Requeued} ticket(s) requeued, {Flagged} in-progress " +
            "ticket(s) left for manual review, {Assigned} auto-assigned.",
            userId, assignedTickets.Count, flagged, autoAssigned);

        return new AgentDeactivationResult
        {
            RequeuedTicketCount = assignedTickets.Count,
            FlaggedTicketCount = flagged,
            AutoAssignedTicketCount = autoAssigned
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task EnsureAgentRoleAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!await _userManager.IsInRoleAsync(user, "Agent"))
            throw new InvalidOperationException("Only Agents can manage their work profile.");
    }

    private async Task<IReadOnlyList<int>> ValidateCategoryIdsAsync(IEnumerable<int>? categoryIds)
    {
        var ids = (categoryIds ?? Enumerable.Empty<int>()).Distinct().ToList();

        if (ids.Count == 0)
            throw new ArgumentException("Select at least one category.");

        var activeCount = await _db.Categories
            .CountAsync(c => ids.Contains(c.Id) && c.IsActive);

        if (activeCount != ids.Count)
            throw new ArgumentException(
                "One or more selected categories do not exist or are inactive.");

        return ids;
    }
}