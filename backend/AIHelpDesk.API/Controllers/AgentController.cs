
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

[ApiController]
[Route("api/agent")]
[Authorize(Roles = "Agent")]
public class AgentController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var email = User.FindFirstValue(
            ClaimTypes.Email);

        return Ok(new
        {
            message = "Agent profile retrieved successfully.",
            userId,
            email,
            role = "Agent"
        });
    }
}
