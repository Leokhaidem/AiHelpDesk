
using AIHelpDesk.Application.Agent.DTOs;

namespace AIHelpDesk.Application.Agent.Services;

public interface IAgentService
{
    Task<IReadOnlyList<AssignedTicketResponse>>
        GetAssignedTicketsAsync(string agentId);

    Task<AssignedTicketResponse?> GetTicketByIdAsync(
        int ticketId,
        string agentId);

    Task<bool> UpdateTicketStatusAsync(
        int ticketId,
        string agentId,
        UpdateTicketStatusRequest request);

    Task<bool> ResolveTicketAsync(
        int ticketId,
        string agentId,
        ResolveTicketRequest request);
}
