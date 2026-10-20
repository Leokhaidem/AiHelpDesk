using System.Security.Claims;
using AIHelpDesk.Application.Tickets.DTOs;
using AIHelpDesk.Application.Tickets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Ticket endpoints for Support Agents (assigned tickets only).
/// </summary>
[ApiController]
[Route("api/tickets/agent")]
[Authorize(Roles = "Agent")]
public class AgentTicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public AgentTicketsController(ITicketService ticketService)
        => _ticketService = ticketService;

    private string AgentId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>List tickets assigned to you.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TicketListQuery query, CancellationToken ct)
    {
        var result = await _ticketService.GetAssignedTicketsAsync(AgentId, query, ct);
        return Ok(result);
    }

    /// <summary>Get details of a ticket assigned to you.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var ticket = await _ticketService.GetAssignedTicketAsync(id, AgentId, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    /// <summary>Start working on an assigned ticket (Assigned → InProgress).</summary>
    [HttpPut("{id:int}/start")]
    public async Task<IActionResult> Start(int id, CancellationToken ct)
    {
        await _ticketService.StartWorkAsync(id, AgentId, ct);
        return Ok(new { message = "Ticket is now In Progress." });
    }

    /// <summary>Record the resolution for a ticket (InProgress → Resolved).</summary>
    [HttpPut("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(
        int id, [FromBody] ResolveTicketRequest request, CancellationToken ct)
    {
        await _ticketService.ResolveAsync(id, AgentId, request, ct);
        return Ok(new { message = "Ticket resolved successfully." });
    }

    /// <summary>Close a resolved ticket (Resolved → Closed).</summary>
    [HttpPut("{id:int}/close")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        await _ticketService.CloseAsync(id, AgentId, ct);
        return Ok(new { message = "Ticket closed successfully." });
    }

    /// <summary>Hand back an unstarted ticket (Assigned → AwaitingAssignment).</summary>
    [HttpPut("{id:int}/release")]
    public async Task<IActionResult> Release(int id, CancellationToken ct)
    {
        await _ticketService.ReleaseAsync(id, AgentId, ct);
        return Ok(new { message = "Ticket released back to the assignment queue." });
    }
}
