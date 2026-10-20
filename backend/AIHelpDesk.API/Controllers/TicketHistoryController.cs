using AIHelpDesk.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Ticket audit history (stored in MongoDB). Accessible by ticket stakeholders.
/// </summary>
[ApiController]
[Route("api/tickets/{ticketId:int}/history")]
[Authorize]
public class TicketHistoryController : ControllerBase
{
    private readonly ITicketHistoryRepository _history;

    public TicketHistoryController(ITicketHistoryRepository history)
        => _history = history;

    /// <summary>Get the full audit trail for a ticket (Admin only).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get(int ticketId, CancellationToken ct)
    {
        var events = await _history.GetByTicketIdAsync(ticketId, ct);
        return Ok(events);
    }
}
