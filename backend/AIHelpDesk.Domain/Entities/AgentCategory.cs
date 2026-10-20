namespace AIHelpDesk.Domain.Entities;

/// <summary>
/// Many-to-many link: which categories an agent works on.
/// </summary>
public class AgentCategory
{
    public string AgentId { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category? Category { get; set; }
}