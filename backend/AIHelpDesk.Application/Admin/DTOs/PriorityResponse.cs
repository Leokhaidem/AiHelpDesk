namespace AIHelpDesk.Application.Admin.DTOs;

public class PriorityResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Level { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}