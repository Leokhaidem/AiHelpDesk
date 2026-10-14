
using System.Security.Claims;
using AIHelpDesk.Application.Agent.DTOs;
using AIHelpDesk.Application.Agent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

[ApiController]
[Route("api/agent/tickets")]
[Authorize(Roles = "Agent")]
public class AgentTicketsController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentTicketsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    private string? GetAgentId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }



    [HttpGet]
    public async Task<IActionResult> GetAssignedTickets()
    {
        var agentId = GetAgentId();

        if (string.IsNullOrEmpty(agentId))
            return Unauthorized();

        var tickets =
            await _agentService.GetAssignedTicketsAsync(agentId);

        return Ok(tickets);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTicketById(int id)
    {
        var agentId = GetAgentId();

        if (string.IsNullOrEmpty(agentId))
            return Unauthorized();

        var ticket = await _agentService.GetTicketByIdAsync(id, agentId);

        if (ticket is null)
            return NotFound();

        return Ok(ticket);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateTicketStatus(
        int id,
        [FromBody] UpdateTicketStatusRequest request)
    {
        var agentId = GetAgentId();

        if (string.IsNullOrEmpty(agentId))
            return Unauthorized();

        try
        {
            var updated = await _agentService.UpdateTicketStatusAsync(
                id, agentId, request);

            if (!updated)
                return NotFound();

            return Ok(new { message = "Ticket status updated successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/resolve")]
    public async Task<IActionResult> ResolveTicket(
        int id,
        [FromBody] ResolveTicketRequest request)
    {
        var agentId = GetAgentId();

        if (string.IsNullOrEmpty(agentId))
            return Unauthorized();

        try
        {
            var resolved = await _agentService.ResolveTicketAsync(
                id, agentId, request);

            if (!resolved)
                return NotFound();

            return Ok(new { message = "Ticket resolved successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
