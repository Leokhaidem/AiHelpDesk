using System.Security.Claims;
using AIHelpDesk.Application.Admin.DTOs;
using AIHelpDesk.Application.AgentApplications.DTOs;
using AIHelpDesk.Application.AgentApplications.Services;
using AIHelpDesk.Application.AgentProfile.DTOs;
using AIHelpDesk.Application.AgentProfile.Services;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IAgentProfileService _profileService;
    private readonly IAgentApplicationService _applicationService;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IAgentProfileService profileService,
        IAgentApplicationService applicationService)
    {
        _userManager = userManager;
        _db = db;
        _profileService = profileService;
        _applicationService = applicationService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserResponse>>> GetUsers(
        [FromQuery] string? role)
    {
        IQueryable<ApplicationUser> query = _userManager.Users;

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleId = await _db.Roles
                .AsNoTracking()
                .Where(r => r.Name == role)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (roleId is null)
                return Ok(new List<AdminUserResponse>());

            query = query.Where(u => _db.UserRoles.Any(
                ur => ur.UserId == u.Id && ur.RoleId == roleId));
        }

        var users = await query.OrderBy(u => u.FullName).ToListAsync();

        var response = new List<AdminUserResponse>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            response.Add(new AdminUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                IsActive = user.IsActive,
                Roles = roles
            });
        }

        var agentIds = response
            .Where(r => r.Roles.Contains("Agent"))
            .Select(r => r.Id)
            .ToList();

        if (agentIds.Count > 0)
        {
            // Batch the agent enrichment data in a few queries.
            var categoryLinks = await _db.AgentCategories
                .AsNoTracking()
                .Where(ac => agentIds.Contains(ac.AgentId))
                .ToListAsync();

            var categoryIds = categoryLinks.Select(l => l.CategoryId).Distinct().ToList();

            var categories = await _db.Categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .Select(c => new CategoryRefResponse { Id = c.Id, Name = c.Name })
                .ToListAsync();

            var openCounts = await _db.Tickets
                .AsNoTracking()
                .Where(t => agentIds.Contains(t.AssignedAgentId)
                    && (t.Status == TicketStatus.Assigned || t.Status == TicketStatus.InProgress))
                .GroupBy(t => t.AssignedAgentId!)
                .Select(g => new { AgentId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var userResponse in response)
            {
                if (!userResponse.Roles.Contains("Agent"))
                    continue;

                userResponse.Categories = categoryLinks
                    .Where(link => link.AgentId == userResponse.Id)
                    .Select(link => categories.FirstOrDefault(c => c.Id == link.CategoryId))
                    .Where(c => c is not null)
                    .Select(c => c!)
                    .ToList();

                userResponse.OpenTicketCount = openCounts
                    .FirstOrDefault(count => count.AgentId == userResponse.Id)?.Count ?? 0;
            }
        }

        return Ok(response);
    }
    
    [HttpPost("users")]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        CreateAdminUserRequest request)
    {
        var email = request.Email.Trim();

        if (request.Role == "Agent" && request.CategoryIds is not { Count: > 0 })
        {
            return BadRequest(new
            {
                errors = new[] { "CategoryIds is required when creating an Agent user." }
            });
        }

        if (request.Role == "Agent")
            await _applicationService.ValidateCategoriesAsync(request.CategoryIds);

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return Conflict(new
            {
                message = "A user with this email already exists."
            });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            IsActive = true,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = createResult.Errors.Select(error => error.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            request.Role);

        if (!roleResult.Succeeded)
        {
            // Roll back the account if its requested role cannot be assigned.
            await _userManager.DeleteAsync(user);

            return BadRequest(new
            {
                errors = roleResult.Errors.Select(error => error.Description)
            });
        }

        IList<CategoryRefResponse>? categories = null;

        if (request.Role == "Agent")
            categories = (await _profileService.EnsureAgentAsync(user.Id, request.CategoryIds!)).ToList();

        var response = new AdminUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            IsActive = user.IsActive,
            Roles = new List<string> { request.Role },
            Categories = categories
        };

        return CreatedAtAction(
            nameof(GetUsers),
            new { },
            response);
    }

    
    [HttpPut("users/{id}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        string id,
        UpdateAdminUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var email = request.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null && existingUser.Id != user.Id)
        {
            return Conflict(new
            {
                message = "Another user already uses this email."
            });
        }

        user.FullName = request.FullName.Trim();

        // Update the username and email through ASP.NET Identity.
        user.UserName = email;
        user.Email = email;

        // Require email confirmation again after changing the address.
        user.EmailConfirmed = false;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(error => error.Description)
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AdminUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Roles = roles
        });
    }

    [HttpPatch("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        string id,
        UpdateUserStatusRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        // Identify the Admin making this request.
        var currentAdminId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        // Prevent an Admin from deactivating their own account.
        if (!request.IsActive && user.Id == currentAdminId)
        {
            return BadRequest(new
            {
                message = "You cannot deactivate your own account."
            });
        }

        // Protect against deactivating the last active Admin.
        var targetIsAdmin = await _userManager.IsInRoleAsync(
            user,
            "Admin");

        if (!request.IsActive && targetIsAdmin && user.IsActive)
        {
            var activeAdmins = await _userManager.GetUsersInRoleAsync(
                "Admin");

            var activeAdminCount = activeAdmins.Count(admin => admin.IsActive);

            if (activeAdminCount <= 1)
            {
                return BadRequest(new
                {
                    message = "You cannot deactivate the last active Admin."
                });
            }
        }

        // Flow I: deactivating an Agent also requeues the open Assigned tickets.
        AgentDeactivationResult? agentDeactivation = null;

        if (!request.IsActive && await _userManager.IsInRoleAsync(user, "Agent"))
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            agentDeactivation = await _profileService.DeactivateAgentAsync(user.Id, adminId);
        }

        user.IsActive = request.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(error => error.Description)
            });
        }

        return Ok(new
        {
            message = request.IsActive
                ? "User activated successfully."
                : "User deactivated successfully.",
            userId = user.Id,
            isActive = user.IsActive,
            agentDeactivation
        });
    }
    
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(
        string id,
        UpdateUserRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        // This endpoint manages Employee and Agent accounts only.
        if (currentRoles.Contains("Admin"))
        {
            return BadRequest(new
            {
                message = "Admin roles cannot be changed through this endpoint."
            });
        }

        if (!await _userManager.IsInRoleAsync(user, request.Role))
        {
            // Remove existing Employee/Agent roles before assigning the new one.
            var rolesToRemove = currentRoles
                .Where(role => role == "Employee" || role == "Agent")
                .ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user,
                    rolesToRemove);

                if (!removeResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        errors = removeResult.Errors.Select(
                            error => error.Description)
                    });
                }
            }

            var addResult = await _userManager.AddToRoleAsync(
                user,
                request.Role);

            if (!addResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = addResult.Errors.Select(
                        error => error.Description)
                });
            }
        }

        // Flow B / Flow I: keep the agent work profile in sync with the role.
        AgentDeactivationResult? agentDeactivation = null;

        if (request.Role == "Agent")
        {
            if (request.CategoryIds is { Count: > 0 })
                await _applicationService.ValidateCategoriesAsync(request.CategoryIds);

            await _profileService.EnsureAgentAsync(user.Id, request.CategoryIds);
        }
        else if (currentRoles.Contains("Agent"))
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            agentDeactivation = await _profileService.DeactivateAgentAsync(user.Id, adminId);
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            message = "User role updated successfully.",
            userId = user.Id,
            roles = updatedRoles,
            agentDeactivation
        });
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<AdminUserResponse>> GetUserById(
        string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var response = new AdminUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Roles = roles
        };

        return Ok(response);
    }

    [HttpGet("users/{id}/categories")]
    public async Task<IActionResult> GetUserCategories(string id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(await _profileService.GetCategoriesAsync(user.Id, ct));
    }

    [HttpPut("users/{id}/categories")]
    public async Task<IActionResult> SetUserCategories(
        string id, [FromBody] SetAgentCategoriesRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (!await _userManager.IsInRoleAsync(user, "Agent"))
        {
            return BadRequest(new
            {
                message = "Only Agent users have work categories."
            });
        }

        return Ok(await _profileService.SetCategoriesAsync(user.Id, request.CategoryIds, ct));
    }

    [HttpGet("agent-applications")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await _applicationService.GetApplicationsAsync(status, page, pageSize, ct));
    }

    [HttpGet("agent-applications/{id:int}")]
    public async Task<IActionResult> GetApplication(int id, CancellationToken ct)
    {
        var application = await _applicationService.GetApplicationAsync(id, ct);
        return application is null ? NotFound() : Ok(application);
    }

    [HttpPost("agent-applications/{id:int}/approve")]
    public async Task<IActionResult> ApproveApplication(
        int id, [FromBody] ApproveAgentApplicationRequest? request, CancellationToken ct)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _applicationService.ApproveAsync(id, request, adminId, ct));
    }

    [HttpPost("agent-applications/{id:int}/reject")]
    public async Task<IActionResult> RejectApplication(
        int id, [FromBody] RejectAgentApplicationRequest? request, CancellationToken ct)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _applicationService.RejectAsync(id, request, adminId, ct));
    }
}