
using AIHelpDesk.Application.Agent.Services;
using AIHelpDesk.Application.Agent.DTOs;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Services;

public class AgentService : IAgentService
{
    private readonly AppDbContext _context;

    public AgentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AssignedTicketResponse>>
        GetAssignedTicketsAsync(string agentId)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedAgentId == agentId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AssignedTicketResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Summary = t.Summary,
                SuggestedResolution = t.SuggestedResolution,
                Status = t.Status,
                Priority = t.Priority,
                Category = t.Category,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<AssignedTicketResponse?> GetTicketByIdAsync(
        int ticketId,
        string agentId)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Id == ticketId &&
                        t.AssignedAgentId == agentId)
            .Select(t => new AssignedTicketResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Summary = t.Summary,
                SuggestedResolution = t.SuggestedResolution,
                Status = t.Status,
                Priority = t.Priority,
                Category = t.Category,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateTicketStatusAsync(
        int ticketId,
        string agentId,
        UpdateTicketStatusRequest request)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t =>
                t.Id == ticketId &&
                t.AssignedAgentId == agentId);

        if (ticket is null)
            return false;

        var allowedStatuses = new[]
        {
            "Open", "InProgress", "Resolved", "Closed"
        };

        if (!allowedStatuses.Contains(request.Status))
            throw new ArgumentException("Invalid ticket status.");

        ticket.Status = request.Status;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResolveTicketAsync(
        int ticketId,
        string agentId,
        ResolveTicketRequest request)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t =>
                t.Id == ticketId &&
                t.AssignedAgentId == agentId);

        if (ticket is null)
            return false;

        if (string.IsNullOrWhiteSpace(request.Resolution))
            throw new ArgumentException("Resolution is required.");

        ticket.Resolution = request.Resolution;
        ticket.Status = "Resolved";
        ticket.ResolvedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
