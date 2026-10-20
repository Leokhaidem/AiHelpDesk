using AIHelpDesk.Application.AgentApplications.DTOs;

namespace AIHelpDesk.Application.Auth.DTOs;

public class RegisterResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Null when the user did not apply to become an agent.</summary>
    public AgentApplicationResponse? AgentApplication { get; set; }
}