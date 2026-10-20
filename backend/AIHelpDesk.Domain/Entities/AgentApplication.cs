using AIHelpDesk.Domain.Enums;

namespace AIHelpDesk.Domain.Entities;

/// <summary>
/// A request by an Employee to become a support Agent. Reviewed by an Admin.
/// A user can have only one Pending application at a time.
/// </summary>
public class AgentApplication
{
    public int Id { get; set; }

    /// <summary>The applicant (Identity user ID).</summary>
    public string UserId { get; set; } = string.Empty;

    public string? Message { get; set; }

    public AgentApplicationStatus Status { get; set; } = AgentApplicationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Identity user ID of the Admin who approved/rejected it.</summary>
    public string? ReviewedByAdminId { get; set; }

    public string? ReviewNote { get; set; }

    /// <summary>Categories the applicant asked to work on.</summary>
    public ICollection<AgentApplicationCategory> Categories { get; set; }
        = new List<AgentApplicationCategory>();
}