namespace AIHelpDesk.Domain.Entities;

/// <summary>
/// Represents a ticket priority level. Soft-deleted via IsActive.
/// </summary>
public class Priority
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Lower value = higher urgency (e.g. 1=Critical, 2=High, 3=Medium, 4=Low).</summary>
    public int Level { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}