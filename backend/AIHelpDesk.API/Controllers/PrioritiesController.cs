
using AIHelpDesk.Application.Admin.DTOs;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.API.Controllers;

[ApiController]
[Route("api/admin/priorities")]
[Authorize(Roles = "Admin")]
public class PrioritiesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PrioritiesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<PriorityResponse>>> GetPriorities()
    {
        var priorities = await _dbContext.Priorities
            .AsNoTracking()
            .OrderBy(priority => priority.Level)
            .Select(priority => new PriorityResponse
            {
                Id = priority.Id,
                Name = priority.Name,
                Level = priority.Level,
                IsActive = priority.IsActive,
                CreatedAt = priority.CreatedAt
            })
            .ToListAsync();

        return Ok(priorities);
    }

    // [HttpGet("{id:int}")]
    // public async Task<ActionResult<PriorityResponse>> GetPriorityById(int id)
    // {
    //     var priority = await _dbContext.Priorities
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync(priority => priority.Id == id);

    //     if (priority is null)
    //     {
    //         return NotFound(new { message = "Priority not found." });
    //     }

    //     return Ok(new PriorityResponse
    //     {
    //         Id = priority.Id,
    //         Name = priority.Name,
    //         Level = priority.Level,
    //         IsActive = priority.IsActive,
    //         CreatedAt = priority.CreatedAt
    //     });
    // }

    // [HttpPost]
    // public async Task<ActionResult<PriorityResponse>> CreatePriority(
    //     CreatePriorityRequest request)
    // {
    //     var name = request.Name.Trim();

    //     var duplicateName = await _dbContext.Priorities
    //         .AnyAsync(priority => priority.Name.ToLower() == name.ToLower());

    //     if (duplicateName)
    //     {
    //         return Conflict(new
    //         {
    //             message = "A priority with this name already exists."
    //         });
    //     }

    //     var duplicateLevel = await _dbContext.Priorities
    //         .AnyAsync(priority => priority.Level == request.Level);

    //     if (duplicateLevel)
    //     {
    //         return Conflict(new
    //         {
    //             message = "A priority with this level already exists."
    //         });
    //     }

    //     var priority = new Priority
    //     {
    //         Name = name,
    //         Level = request.Level,
    //         IsActive = true,
    //         CreatedAt = DateTime.UtcNow
    //     };

    //     _dbContext.Priorities.Add(priority);
    //     await _dbContext.SaveChangesAsync();

    //     var response = new PriorityResponse
    //     {
    //         Id = priority.Id,
    //         Name = priority.Name,
    //         Level = priority.Level,
    //         IsActive = priority.IsActive,
    //         CreatedAt = priority.CreatedAt
    //     };

    //     return CreatedAtAction(
    //         nameof(GetPriorityById),
    //         new { id = priority.Id },
    //         response);
    // }

    // [HttpPut("{id:int}")]
    // public async Task<ActionResult<PriorityResponse>> UpdatePriority(
    //     int id,
    //     UpdatePriorityRequest request)
    // {
    //     var priority = await _dbContext.Priorities.FindAsync(id);

    //     if (priority is null)
    //     {
    //         return NotFound(new { message = "Priority not found." });
    //     }

    //     var name = request.Name.Trim();

    //     var duplicateName = await _dbContext.Priorities
    //         .AnyAsync(p =>
    //             p.Id != id &&
    //             p.Name.ToLower() == name.ToLower());

    //     if (duplicateName)
    //     {
    //         return Conflict(new
    //         {
    //             message = "Another priority already uses this name."
    //         });
    //     }

    //     var duplicateLevel = await _dbContext.Priorities
    //         .AnyAsync(p => p.Id != id && p.Level == request.Level);

    //     if (duplicateLevel)
    //     {
    //         return Conflict(new
    //         {
    //             message = "Another priority already uses this level."
    //         });
    //     }

    //     priority.Name = name;
    //     priority.Level = request.Level;

    //     await _dbContext.SaveChangesAsync();

    //     return Ok(new PriorityResponse
    //     {
    //         Id = priority.Id,
    //         Name = priority.Name,
    //         Level = priority.Level,
    //         IsActive = priority.IsActive,
    //         CreatedAt = priority.CreatedAt
    //     });
    // }

    // [HttpPatch("{id:int}/status")]
    // public async Task<IActionResult> UpdatePriorityStatus(
    //     int id,
    //     UpdateCategoryStatusRequest request)
    // {
    //     var priority = await _dbContext.Priorities.FindAsync(id);

    //     if (priority is null)
    //     {
    //         return NotFound(new { message = "Priority not found." });
    //     }

    //     priority.IsActive = request.IsActive;
    //     await _dbContext.SaveChangesAsync();

    //     return Ok(new
    //     {
    //         message = request.IsActive
    //             ? "Priority activated successfully."
    //             : "Priority deactivated successfully.",
    //         priorityId = priority.Id,
    //         isActive = priority.IsActive
    //     });
    // }
}
