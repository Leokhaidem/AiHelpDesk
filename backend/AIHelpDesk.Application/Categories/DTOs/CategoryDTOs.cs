namespace AIHelpDesk.Application.Categories.DTOs;

public sealed class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateCategoryStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>An agent who works a category (resolved from AgentCategory links).</summary>
public sealed class CategoryAgentRef
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    /// <summary>All agents working this category (replaces the old single default agent).</summary>
    public IReadOnlyList<CategoryAgentRef> Agents { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
