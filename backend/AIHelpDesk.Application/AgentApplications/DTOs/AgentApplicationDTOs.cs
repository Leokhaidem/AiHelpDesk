using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.AgentApplications.DTOs;

public sealed class CreateAgentApplicationRequest
{
    public List<int> CategoryIds { get; set; } = new();

    [MaxLength(1000)]
    public string? Message { get; set; }
}

public sealed class CategoryRefResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class AgentApplicationResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Pending, Approved, Rejected or Withdrawn.</summary>
    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }
    public IReadOnlyList<CategoryRefResponse> Categories { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

/// <summary>Admin view of an application, including the applicant and the review outcome.</summary>
public sealed class AdminAgentApplicationResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;

    /// <summary>Pending, Approved, Rejected or Withdrawn.</summary>
    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }
    public IReadOnlyList<CategoryRefResponse> Categories { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

/// <summary>Body of POST /admin/agent-applications/{id}/approve.</summary>
public sealed class ApproveAgentApplicationRequest
{
    /// <summary>Overrides the categories the applicant requested; omit to accept them as-is.</summary>
    public List<int>? CategoryIds { get; set; }

    /// <summary>Optional note stored with the decision.</summary>
    public string? Note { get; set; }
}

/// <summary>Body of POST /admin/agent-applications/{id}/reject.</summary>
public sealed class RejectAgentApplicationRequest
{
    public string? Note { get; set; }
}