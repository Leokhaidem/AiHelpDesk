
namespace AIHelpDesk.Domain.Entities;

public class Ticket
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string SuggestedResolution { get; set; } = string.Empty;

    public string Status { get; set; } = "Created";

    public string Priority { get; set; } = "Medium";

    public string Category { get; set; } = string.Empty;

    // Identity user who submitted the ticket
    public string EmployeeId { get; set; } = string.Empty;

    // Identity user assigned to handle the ticket
    public string? AssignedAgentId { get; set; }

    public string? Resolution { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }
}
