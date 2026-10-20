namespace AIHelpDesk.Domain.Entities;

/// <summary>
/// Represents a support ticket category. Soft-deleted via IsActive.
/// The agents who work a category are tracked through AgentCategory rows.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
