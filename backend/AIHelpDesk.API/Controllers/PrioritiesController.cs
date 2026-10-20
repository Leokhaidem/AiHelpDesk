using AIHelpDesk.Application.Priorities.DTOs;
using AIHelpDesk.Application.Priorities.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Priority management — Admin only for writes; GET is open to all authenticated users.
/// </summary>
[ApiController]
[Route("api/priorities")]
[Authorize]
public class PrioritiesController : ControllerBase
{
    private readonly IPriorityService _priorityService;

    public PrioritiesController(IPriorityService priorityService)
        => _priorityService = priorityService;

    /// <summary>List all priorities ordered by level. Pass ?activeOnly=true for active only.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _priorityService.GetAllAsync(activeOnly, ct);
        return Ok(result);
    }

    /// <summary>Get a priority by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _priorityService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new priority (Admin only). Invalidates the lookup cache.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreatePriorityRequest request, CancellationToken ct)
    {
        var result = await _priorityService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Update a priority (Admin only). Invalidates the lookup cache.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdatePriorityRequest request, CancellationToken ct)
    {
        var result = await _priorityService.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Activate or deactivate a priority (Admin only). Invalidates the lookup cache.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(
        int id, [FromBody] UpdatePriorityStatusRequest request, CancellationToken ct)
    {
        var result = await _priorityService.SetStatusAsync(id, request.IsActive, ct);
        return Ok(result);
    }
}
