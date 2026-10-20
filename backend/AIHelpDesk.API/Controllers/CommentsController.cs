using System.Security.Claims;
using AIHelpDesk.Application.Comments.DTOs;
using AIHelpDesk.Application.Comments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Comments on tickets. Authorization is enforced in the service layer.
/// Employees can comment on their own tickets; agents on assigned; admins on all.
/// </summary>
[ApiController]
[Route("api/tickets/{ticketId:int}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
        => _commentService = commentService;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string UserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    /// <summary>Get all comments for a ticket.</summary>
    [HttpGet]
    public async Task<IActionResult> List(int ticketId, CancellationToken ct)
    {
        var comments = await _commentService.GetByTicketIdAsync(ticketId, UserId, UserRole, ct);
        return Ok(comments);
    }

    /// <summary>Add a comment to a ticket.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        int ticketId, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var comment = await _commentService.AddCommentAsync(ticketId, UserId, UserRole, request, ct);
        return CreatedAtAction(nameof(List), new { ticketId }, comment);
    }
}
