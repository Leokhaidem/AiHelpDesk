using AIHelpDesk.API.Services;
using AIHelpDesk.Application.AgentApplications.DTOs;
using AIHelpDesk.Application.AgentApplications.Services;
using AIHelpDesk.Application.Auth.DTOs;
using AIHelpDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IAgentApplicationService _applications;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IAgentApplicationService applications)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _applications = applications;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive)
        {
            return Unauthorized("Invalid email or password.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(_tokenService.CreateToken(user.Id, user.Email!, roles));
    }

    /// <summary>
    /// Public self-registration. Always creates an Employee.
    /// If ApplyAsAgent is true, a Pending agent application is created too;
    /// an Admin must approve it before the user becomes an Agent.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();

        // Validate the requested categories BEFORE creating the account,
        // so a bad category list never leaves a half-created user behind.
        IReadOnlyList<int> categoryIds = [];
        if (request.ApplyAsAgent)
        {
            categoryIds = await _applications.ValidateCategoriesAsync(request.CategoryIds, ct);
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Conflict("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName?.Trim() ?? string.Empty,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Employee");

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return StatusCode(500, "Unable to assign the Employee role.");
        }

        AgentApplicationResponse? application = null;

        if (request.ApplyAsAgent)
        {
            try
            {
                application = await _applications.CreateAsync(
                    user.Id,
                    new CreateAgentApplicationRequest
                    {
                        CategoryIds = categoryIds.ToList(),
                        Message = request.AgentMessage
                    },
                    ct);
            }
            catch
            {
                // Don't leave an account behind if the application could not be saved.
                await _userManager.DeleteAsync(user);
                throw;
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.CreateToken(user.Id, user.Email!, roles);

        return Ok(new RegisterResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            AgentApplication = application
        });
    }
}