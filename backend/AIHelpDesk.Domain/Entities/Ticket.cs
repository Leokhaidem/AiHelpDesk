using AIHelpDesk.Domain.Enums;

namespace AIHelpDesk.Domain.Entities;

/// <summary>
/// Core support ticket entity. Relates to Category and Priority via FK IDs.
/// AI-produced names (CategoryName, PriorityName) are stored as strings for display/history.
/// </summary>
public class Ticket
{
    public int Id { get; set; }

    /// <summary>Short, meaningful title provided by the employee.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full problem description provided by the employee.</summary>
    public string Description { get; set; } = string.Empty;

    // ─── AI-produced fields ───────────────────────────────────────────────────

    /// <summary>AI-generated short summary of the problem.</summary>
    public string? AiSummary { get; set; }

    /// <summary>AI-generated suggested resolution for the agent.</summary>
    public string? AiSuggestedResolution { get; set; }

    // ─── Classification (set by AI, validated against configured lists) ───────

    /// <summary>FK to Category. Set after successful AI analysis.</summary>
    public int? CategoryId { get; set; }

    /// <summary>FK to Priority. Set after successful AI analysis.</summary>
    public int? PriorityId { get; set; }

    // ─── Actor references (Identity user IDs) ─────────────────────────────────

    /// <summary>Identity user ID of the employee who submitted this ticket.</summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>Identity user ID of the support agent assigned to work on this ticket.</summary>
    public string? AssignedAgentId { get; set; }

    // ─── Workflow state ────────────────────────────────────────────────────────

    public TicketStatus Status { get; set; } = TicketStatus.Created;

    /// <summary>Resolution text recorded by the agent when resolving the ticket.</summary>
    public string? Resolution { get; set; }

    // ─── Timestamps ───────────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    // ─── Navigation properties ────────────────────────────────────────────────

    public Category? Category { get; set; }

    public Priority? Priority { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
