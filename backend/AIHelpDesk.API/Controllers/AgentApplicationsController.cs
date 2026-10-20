using System.Security.Claims;
using AIHelpDesk.Application.AgentApplications.DTOs;
using AIHelpDesk.Application.AgentApplications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Endpoints for an Employee to apply for the Agent role and manage the
/// own application. Admin review endpoints live in AdminController.
/// </summary>
[ApiController]
[Route("api/agent-applications")]
[Authorize(Roles = "Employee")]
public class AgentApplicationsController : ControllerBase
{
    private readonly IAgentApplicationService _applicationService;

    public AgentApplicationsController(IAgentApplicationService applicationService)
        => _applicationService = applicationService;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Apply to become an Agent.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAgentApplicationRequest request, CancellationToken ct)
    {
        var application = await _applicationService.CreateAsync(UserId, request, ct);
        return CreatedAtAction(nameof(GetMyApplication), new { }, application);
    }

    /// <summary>The own most recent application (any status). 404 when never applied.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyApplication(CancellationToken ct)
    {
        var application = await _applicationService.GetMyApplicationAsync(UserId, ct);
        return application is null ? NotFound() : Ok(application);
    }

    /// <summary>Withdraw the own pending application.</summary>
    [HttpDelete("mine")]
    public async Task<IActionResult> Withdraw(CancellationToken ct)
    {
        await _applicationService.WithdrawAsync(UserId, ct);
        return Ok(new { message = "Application withdrawn." });
    }
}
