namespace AIHelpDesk.Domain.Entities;

public class AgentApplicationCategory
{
    public int ApplicationId { get; set; }

    public int CategoryId { get; set; }

    public AgentApplication? Application { get; set; }

    public Category? Category { get; set; }
}