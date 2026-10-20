namespace AIHelpDesk.Domain.Enums;

public enum TicketStatus
{
    Created = 0,
    AnalysisInProgress = 1,
    Assigned = 2,
    InProgress = 3,
    Resolved = 4,
    Closed = 5,
    AnalysisFailed = 6,

    /// <summary>
    /// AI analysis succeeded but no eligible agent was available.
    /// An Admin (or the assignment sweep) must assign an agent.
    /// </summary>
    AwaitingAssignment = 7
}