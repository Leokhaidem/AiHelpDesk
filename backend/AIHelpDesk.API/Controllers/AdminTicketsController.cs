using System.Security.Claims;
using AIHelpDesk.Application.Tickets.DTOs;
using AIHelpDesk.Application.Tickets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Admin-only endpoints: view all tickets, assign agents, override AI analysis, retry analysis.
/// </summary>
[ApiController]
[Route("api/admin/tickets")]
[Authorize(Roles = "Admin")]
public class AdminTicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public AdminTicketsController(ITicketService ticketService)
        => _ticketService = ticketService;

    private string AdminId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>List all tickets with optional filters and pagination.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TicketListQuery query, CancellationToken ct)
    {
        var result = await _ticketService.GetAllTicketsAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Get full details of any ticket.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    /// <summary>Manually assign a support agent to a ticket.</summary>
    [HttpPut("{id:int}/assign")]
    public async Task<IActionResult> Assign(
        int id, [FromBody] AdminAssignAgentRequest request, CancellationToken ct)
    {
        await _ticketService.AssignAgentAsync(id, request, AdminId, ct);
        return Ok(new { message = "Agent assigned successfully." });
    }

    /// <summary>Override AI analysis result for a failed ticket (sets category + priority manually).</summary>
    [HttpPut("{id:int}/override-analysis")]
    public async Task<IActionResult> OverrideAnalysis(
        int id, [FromBody] AdminOverrideAnalysisRequest request, CancellationToken ct)
    {
        await _ticketService.OverrideAnalysisAsync(id, request, AdminId, ct);
        return Ok(new { message = "Analysis overridden successfully." });
    }

    /// <summary>Retry AI analysis for a ticket that previously failed.</summary>
    [HttpPost("{id:int}/retry-analysis")]
    public async Task<IActionResult> RetryAnalysis(int id, CancellationToken ct)
    {
        await _ticketService.RetryAnalysisAsync(id, AdminId, ct);
        return Ok(new { message = "AI analysis queued for retry." });
    }

    /// <summary>Admin can also close any resolved ticket.</summary>
    [HttpPut("{id:int}/close")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        await _ticketService.CloseAsync(id, AdminId, ct);
        return Ok(new { message = "Ticket closed successfully." });
    }
}
