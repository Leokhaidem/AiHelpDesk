namespace AIHelpDesk.Domain.Entities;

/// <summary>
/// Work profile of a support agent. One row per user in the Agent role.
/// </summary>
public class AgentProfile
{
    /// <summary>Identity user ID (AspNetUsers.Id). Also the primary key.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>When false, the agent receives no automatic assignments.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Maximum tickets in Assigned + InProgress before auto-assignment skips this agent.</summary>
    public int MaxOpenTickets { get; set; } = 10;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}