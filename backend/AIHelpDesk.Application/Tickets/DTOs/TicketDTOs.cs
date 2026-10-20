using AIHelpDesk.Domain.Enums;

namespace AIHelpDesk.Application.Tickets.DTOs;

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public sealed class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class UpdateTicketStatusRequest
{
    public TicketStatus Status { get; set; }
}

public sealed class ResolveTicketRequest
{
    public string Resolution { get; set; } = string.Empty;
}

public sealed class AdminAssignAgentRequest
{
    public string AgentId { get; set; } = string.Empty;
}

/// <summary>Admin can manually override category/priority when AI fails.</summary>
public sealed class AdminOverrideAnalysisRequest
{
    public int CategoryId { get; set; }
    public int PriorityId { get; set; }
    public string? AgentId { get; set; }
}

// ─── Response DTOs ────────────────────────────────────────────────────────────

public sealed class TicketSummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? PriorityName { get; set; }
    public string? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class TicketDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AiSummary { get; set; }
    public string? AiSuggestedResolution { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? PriorityId { get; set; }
    public string? PriorityName { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public string? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public sealed record TicketListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public TicketStatus? Status { get; init; }
    public int? CategoryId { get; init; }
    public int? PriorityId { get; init; }
    public string? AssignedAgentId { get; init; }
    public string? EmployeeId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Only tickets still held by an agent who is deactivated or no longer in the
    /// Agent role (result of Flow I deactivation/demotion cleanup).
    /// </summary>
    public bool AgentInactive { get; init; }
}
