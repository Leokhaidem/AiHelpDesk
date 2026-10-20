namespace AIHelpDesk.Application.Priorities.DTOs;

public sealed class CreatePriorityRequest
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}

public sealed class UpdatePriorityRequest
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}

public sealed class UpdatePriorityStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class PriorityResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
