using System.Security.Claims;
using AIHelpDesk.Application.Employee.DTOs;
using AIHelpDesk.Application.Employee.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

[ApiController]
[Route("api/employee/tickets")]
[Authorize(Roles = "Employee")]
public class EmployeeTicketsController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeTicketsController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequest request)
    {
        var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized();

        var ticket = await _employeeService.CreateTicketAsync(
            employeeId, request);

        return CreatedAtAction(
            nameof(GetMyTicket),
            new { id = ticket.Id },
            ticket);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTickets()
    {
        var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized();

        var tickets = await _employeeService.GetMyTicketsAsync(employeeId);

        return Ok(tickets);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMyTicket(int id)
    {
        var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized();

        var ticket = await _employeeService.GetMyTicketByIdAsync(
            id, employeeId);

        if (ticket is null)
            return NotFound();

        return Ok(ticket);
    }
}