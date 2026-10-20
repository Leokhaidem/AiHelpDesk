using AIHelpDesk.Application.AgentApplications.DTOs;

namespace AIHelpDesk.Application.AgentProfile.DTOs;

public sealed class AgentProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int MaxOpenTickets { get; set; }

    /// <summary>Number of tickets in Assigned + InProgress.</summary>
    public int OpenTicketCount { get; set; }

    public IReadOnlyList<CategoryRefResponse> Categories { get; set; } = [];
}

public sealed class SetAgentAvailabilityRequest
{
    public bool IsAvailable { get; set; }

    /// <summary>Optional new capacity cap; must be positive when provided.</summary>
    public int? MaxOpenTickets { get; set; }
}

public sealed class SetAgentCategoriesRequest
{
    public List<int> CategoryIds { get; set; } = new();
}

public sealed class AgentDeactivationResult
{
    /// <summary>Tickets moved from Assigned to AwaitingAssignment.</summary>
    public int RequeuedTicketCount { get; set; }

    /// <summary>InProgress tickets left with the agent for manual review.</summary>
    public int FlaggedTicketCount { get; set; }

    /// <summary>Tickets the post-deactivation sweep assigned to other agents.</summary>
    public int AutoAssignedTicketCount { get; set; }
}