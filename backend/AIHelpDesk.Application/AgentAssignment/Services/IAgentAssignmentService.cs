namespace AIHelpDesk.Application.AgentAssignment.Services;

/// <summary>
/// Flow F — automatic agent assignment.
/// An agent is eligible when: they are in the Agent role with an active account,
/// have an AgentCategory row for the ticket's category, their AgentProfile is
/// available, and they have fewer than MaxOpenTickets in Assigned + InProgress.
/// The eligible agent with the fewest open tickets wins; ties go to the agent
/// who was last assigned a ticket the longest ago.
/// </summary>
public interface IAgentAssignmentService
{
    /// <summary>
    /// Tries to assign an eligible agent to a ticket that is in AnalysisInProgress
    /// or AwaitingAssignment status. On success the agent is set, the ticket moves
    /// to Assigned, and an AgentAssigned history event is written.
    /// </summary>
    /// <param name="excludeAgentId">Agent to skip (e.g. the agent releasing a ticket).</param>
    /// <returns>The assigned agent's user ID, or null when no eligible agent exists
    /// or the ticket is not in an assignable status.</returns>
    Task<string?> TryAssignTicketAsync(int ticketId, string? excludeAgentId = null, CancellationToken ct = default);

    /// <summary>
    /// Re-runs the algorithm over all AwaitingAssignment tickets (oldest first),
    /// optionally limited to a single category. Triggered on application approval,
    /// category/availability changes, deactivation cleanup, release, and when an
    /// agent frees capacity (resolve/close).
    /// </summary>
    /// <returns>The number of tickets that got an agent.</returns>
    Task<int> SweepAsync(int? categoryId = null, string? excludeAgentId = null, CancellationToken ct = default);
}