using AIHelpDesk.Application.Employee.DTOs;
using AIHelpDesk.Application.Employee.Services;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _context;

    public EmployeeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeTicketResponse> CreateTicketAsync(
        string employeeId,
        CreateTicketRequest request)
    {
        var ticket = new Ticket
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            EmployeeId = employeeId,
            Status = "Open",
            Summary = string.Empty,
            SuggestedResolution = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        return MapToResponse(ticket);
    }

    public async Task<IReadOnlyList<EmployeeTicketResponse>> GetMyTicketsAsync(
        string employeeId)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.EmployeeId == employeeId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Select(ticket => MapToResponse(ticket))
            .ToListAsync();
    }

    public async Task<EmployeeTicketResponse?> GetMyTicketByIdAsync(
        int ticketId,
        string employeeId)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(ticket =>
                ticket.Id == ticketId &&
                ticket.EmployeeId == employeeId);

        return ticket is null ? null : MapToResponse(ticket);
    }

    private static EmployeeTicketResponse MapToResponse(Ticket ticket)
    {
        return new EmployeeTicketResponse
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Summary = ticket.Summary,
            SuggestedResolution = ticket.SuggestedResolution,
            Status = ticket.Status,
            Priority = ticket.Priority,
            Category = ticket.Category,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt
        };
    }
}