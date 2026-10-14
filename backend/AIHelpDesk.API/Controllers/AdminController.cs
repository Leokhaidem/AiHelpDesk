using AIHelpDesk.Application.Admin.DTOs;
using AIHelpDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIHelpDesk.Application.Admin.DTOs;
using System.Security.Claims;

namespace AIHelpDesk.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserResponse>>> GetUsers()
    {
        var users = await _userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync();

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

        return Ok(response);
    }
    
    [HttpPost("users")]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        CreateAdminUserRequest request)
    {
        var email = request.Email.Trim();

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

        var response = new AdminUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            IsActive = user.IsActive,
            Roles = new List<string> { request.Role }
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
            isActive = user.IsActive
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

        var updatedRoles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            message = "User role updated successfully.",
            userId = user.Id,
            roles = updatedRoles
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

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new
        {
            message = "You are authorized as an Admin!"
        });
    }
}