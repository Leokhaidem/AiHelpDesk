using System.Security.Claims;
using AIHelpDesk.Application.Tickets.DTOs;
using AIHelpDesk.Application.Tickets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Ticket endpoints accessible to Employees (own tickets only).
/// </summary>
[ApiController]
[Route("api/tickets/employee")]
[Authorize(Roles = "Employee")]
public class EmployeeTicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public EmployeeTicketsController(ITicketService ticketService)
        => _ticketService = ticketService;

    private string EmployeeId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Create a new support ticket. AI analysis is queued automatically.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = await _ticketService.CreateTicketAsync(EmployeeId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    /// <summary>List your own tickets with optional filters.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TicketListQuery query, CancellationToken ct)
    {
        var result = await _ticketService.GetMyTicketsAsync(EmployeeId, query, ct);
        return Ok(result);
    }

    /// <summary>Get details of one of your own tickets.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var ticket = await _ticketService.GetMyTicketAsync(id, EmployeeId, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }
}