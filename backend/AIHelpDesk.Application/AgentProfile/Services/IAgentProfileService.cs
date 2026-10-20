using AIHelpDesk.Application.AgentProfile.DTOs;
using AIHelpDesk.Application.AgentApplications.DTOs;

namespace AIHelpDesk.Application.AgentProfile.Services;

/// <summary>
/// Agent work profile (Flow D): availability, capacity, and the categories an
/// agent works on. Also executes the Flow I cleanup when an agent is deactivated
/// or demoted. Every change that can make tickets (un)assignable ends with an
/// assignment sweep.
/// </summary>
public interface IAgentProfileService
{
    /// <summary>Full profile of the agent (defaults are used while no profile row exists yet).</summary>
    Task<AgentProfileResponse> GetMyProfileAsync(string userId, CancellationToken ct = default);

    /// <summary>The categories the agent works on.</summary>
    Task<IReadOnlyList<CategoryRefResponse>> GetCategoriesAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Replaces the agent's category set (at least one, all active) and runs the assignment sweep.
    /// </summary>
    Task<IReadOnlyList<CategoryRefResponse>> SetCategoriesAsync(
        string userId, IEnumerable<int> categoryIds, CancellationToken ct = default);

    /// <summary>
    /// Updates availability (and optionally the capacity cap) and runs the assignment sweep.
    /// </summary>
    Task<AgentProfileResponse> SetAvailabilityAsync(
        string userId, SetAgentAvailabilityRequest request, CancellationToken ct = default);

    /// <summary>
    /// Ensures the user has an AgentProfile (created with defaults when missing).
    /// When categoryIds are provided, the agent's categories are replaced and the
    /// sweep runs. Used when an Admin creates an agent directly or promotes an employee.
    /// </summary>
    Task<IReadOnlyList<CategoryRefResponse>> EnsureAgentAsync(
        string userId, IEnumerable<int>? categoryIds, CancellationToken ct = default);

    /// <summary>
    /// Flow I — cleanup when an agent is deactivated or demoted: deletes the
    /// AgentCategory rows, marks the profile unavailable (kept for reactivation),
    /// requeues Assigned tickets to AwaitingAssignment (+ sweep), and counts the
    /// InProgress tickets that stay with the agent for manual review via
    /// GET /admin/tickets?agentInactive=true.
    /// </summary>
    Task<AgentDeactivationResult> DeactivateAgentAsync(
        string userId, string adminId, CancellationToken ct = default);
}