
using AIHelpDesk.Application.AgentApplications.DTOs;

namespace AIHelpDesk.Application.Admin.DTOs;

public class AdminUserResponse
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public IList<string> Roles { get; set; } = new List<string>();

    /// <summary>Set for users in the Agent role.</summary>
    public IList<CategoryRefResponse>? Categories { get; set; }

    /// <summary>Assigned + InProgress ticket count. Set for users in the Agent role.</summary>
    public int? OpenTicketCount { get; set; }
}
