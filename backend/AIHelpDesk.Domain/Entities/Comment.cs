namespace AIHelpDesk.Domain.Entities;

/// <summary>
/// A comment posted on a ticket by an employee, agent, or admin.
/// </summary>
public class Comment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    /// <summary>Identity user ID of the comment author.</summary>
    public string AuthorId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Ticket? Ticket { get; set; }
}
