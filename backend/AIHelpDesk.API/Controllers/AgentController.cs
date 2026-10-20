using System.Security.Claims;
using AIHelpDesk.Application.AgentProfile.DTOs;
using AIHelpDesk.Application.AgentProfile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Endpoints for an Agent to inspect and update the own work profile.
/// </summary>
[ApiController]
[Route("api/agent")]
[Authorize(Roles = "Agent")]
public class AgentController : ControllerBase
{
    private readonly IAgentProfileService _profileService;

    public AgentController(IAgentProfileService profileService)
        => _profileService = profileService;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>The own work profile: availability, capacity, open ticket count, categories.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var profile = await _profileService.GetMyProfileAsync(UserId, ct);
        return Ok(profile);
    }

    /// <summary>The categories the agent works on.</summary>
    [HttpGet("me/categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _profileService.GetCategoriesAsync(UserId, ct);
        return Ok(categories);
    }

    /// <summary>Replace the categories the agent works on (at least one required).</summary>
    [HttpPut("me/categories")]
    public async Task<IActionResult> SetCategories(
        [FromBody] SetAgentCategoriesRequest request, CancellationToken ct)
    {
        var categories = await _profileService.SetCategoriesAsync(UserId, request.CategoryIds, ct);
        return Ok(categories);
    }

    /// <summary>Toggle availability / adjust the capacity cap. Triggers an assignment sweep.</summary>
    [HttpPut("me/availability")]
    public async Task<IActionResult> SetAvailability(
        [FromBody] SetAgentAvailabilityRequest request, CancellationToken ct)
    {
        var profile = await _profileService.SetAvailabilityAsync(UserId, request, ct);
        return Ok(profile);
    }
}

