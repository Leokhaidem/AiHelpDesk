using AIHelpDesk.Domain.Enums;

namespace AIHelpDesk.Application.Common;

/// <summary>
/// Enforces allowed ticket status transitions.
/// Invalid transitions throw an <see cref="InvalidOperationException"/> with a human-readable message.
/// </summary>
public static class TicketStateMachine
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions =
        new()
        {
            // Created → Assigned: admin manually assigns before/without AI analysis.
            [TicketStatus.Created]            = [TicketStatus.AnalysisInProgress, TicketStatus.Assigned],

            // AnalysisInProgress → AwaitingAssignment: AI succeeded but no eligible agent.
            [TicketStatus.AnalysisInProgress] = [TicketStatus.Assigned, TicketStatus.AnalysisFailed, TicketStatus.AwaitingAssignment],

            // AnalysisFailed → Assigned: admin override (manual category/priority/agent).
            [TicketStatus.AnalysisFailed]     = [TicketStatus.AnalysisInProgress, TicketStatus.Assigned],

            // AwaitingAssignment → Assigned: the algorithm or an admin picks an agent.
            [TicketStatus.AwaitingAssignment] = [TicketStatus.Assigned],

            // Assigned → AwaitingAssignment: agent releases a ticket, or the agent is
            // deactivated/demoted (Flow I) and the ticket is requeued.
            [TicketStatus.Assigned]           = [TicketStatus.InProgress, TicketStatus.AwaitingAssignment],
            [TicketStatus.InProgress]         = [TicketStatus.Resolved],
            [TicketStatus.Resolved]           = [TicketStatus.Closed],
            [TicketStatus.Closed]             = [],
        };

    /// <summary>
    /// Validates that the requested transition is permitted.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the transition is not allowed.</exception>
    public static void ValidateTransition(TicketStatus current, TicketStatus next)
    {
        if (!AllowedTransitions.TryGetValue(current, out var allowed) ||
            !allowed.Contains(next))
        {
            throw new InvalidOperationException(
                $"Transition from '{current}' to '{next}' is not allowed.");
        }
    }

    /// <summary>
    /// Returns true if the transition is permitted, false otherwise. Does not throw.
    /// </summary>
    public static bool IsAllowed(TicketStatus current, TicketStatus next)
    {
        return AllowedTransitions.TryGetValue(current, out var allowed) &&
               allowed.Contains(next);
    }
}
