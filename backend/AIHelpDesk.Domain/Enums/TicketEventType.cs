namespace AIHelpDesk.Domain.Enums;

public enum TicketEventType
{
    Created = 0,
    StatusChanged = 1,
    AgentAssigned = 2,
    AiAnalysisSucceeded = 3,
    AiAnalysisFailed = 4,
    AiAnalysisRetried = 5,
    CommentAdded = 6,
    AttachmentAdded = 7,
    Resolved = 8,
    Closed = 9,
    CategoryOverridden = 10,
    PriorityOverridden = 11
}
