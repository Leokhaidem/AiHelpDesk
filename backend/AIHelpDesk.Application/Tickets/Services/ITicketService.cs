using AIHelpDesk.Application.Tickets.DTOs;

namespace AIHelpDesk.Application.Tickets.Services;

/// <summary>
/// Core ticket service covering the full ticket lifecycle.
/// All state transitions are validated by <see cref="Common.TicketStateMachine"/>.
/// </summary>
public interface ITicketService
{
    // ─── Employee operations ──────────────────────────────────────────────────

    /// <summary>Creates a ticket and queues it for AI analysis. Returns immediately.</summary>
    Task<TicketDetailResponse> CreateTicketAsync(string employeeId, CreateTicketRequest request, CancellationToken ct = default);

    /// <summary>Returns paginated tickets owned by the employee.</summary>
    Task<PagedResult<TicketSummaryResponse>> GetMyTicketsAsync(string employeeId, TicketListQuery query, CancellationToken ct = default);

    /// <summary>Returns a single ticket owned by the employee, or null if not found/unauthorized.</summary>
    Task<TicketDetailResponse?> GetMyTicketAsync(int ticketId, string employeeId, CancellationToken ct = default);

    // ─── Agent operations ─────────────────────────────────────────────────────

    /// <summary>Returns paginated tickets assigned to the agent.</summary>
    Task<PagedResult<TicketSummaryResponse>> GetAssignedTicketsAsync(string agentId, TicketListQuery query, CancellationToken ct = default);

    /// <summary>Returns a single ticket assigned to the agent, or null if not found/unauthorized.</summary>
    Task<TicketDetailResponse?> GetAssignedTicketAsync(int ticketId, string agentId, CancellationToken ct = default);

    /// <summary>Agent transitions Assigned → InProgress.</summary>
    Task StartWorkAsync(int ticketId, string agentId, CancellationToken ct = default);

    /// <summary>Agent transitions InProgress → Resolved with a resolution text.</summary>
    Task ResolveAsync(int ticketId, string agentId, ResolveTicketRequest request, CancellationToken ct = default);

    /// <summary>
    /// Agent hands back an unstarted ticket (Assigned → AwaitingAssignment).
    /// The assignment sweep immediately re-offers it to other eligible agents,
    /// excluding the releasing agent.
    /// </summary>
    Task ReleaseAsync(int ticketId, string agentId, CancellationToken ct = default);

    /// <summary>Agent or Admin transitions Resolved → Closed.</summary>
    Task CloseAsync(int ticketId, string actorId, CancellationToken ct = default);

    // ─── Admin operations ─────────────────────────────────────────────────────

    /// <summary>Returns all tickets with filtering and pagination.</summary>
    Task<PagedResult<TicketSummaryResponse>> GetAllTicketsAsync(TicketListQuery query, CancellationToken ct = default);

    /// <summary>Returns any ticket by ID (admin view).</summary>
    Task<TicketDetailResponse?> GetTicketByIdAsync(int ticketId, CancellationToken ct = default);

    /// <summary>Admin manually assigns an agent (used when auto-assignment cannot be done).</summary>
    Task AssignAgentAsync(int ticketId, AdminAssignAgentRequest request, string adminId, CancellationToken ct = default);

    /// <summary>Admin overrides AI analysis result when AI fails (sets category, priority, and optionally assigns agent).</summary>
    Task OverrideAnalysisAsync(int ticketId, AdminOverrideAnalysisRequest request, string adminId, CancellationToken ct = default);

    /// <summary>Admin retries AI analysis for a failed ticket.</summary>
    Task RetryAnalysisAsync(int ticketId, string adminId, CancellationToken ct = default);
}
