using AIHelpDesk.Application.Comments.DTOs;
using AIHelpDesk.Application.Comments.Services;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Services;

/// <summary>
/// Manages comments on tickets. Enforces authorization:
/// - Employees may only comment on their own tickets.
/// - Agents may only comment on assigned tickets.
/// - Admins may comment on any ticket.
/// </summary>
public sealed class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CommentService(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<CommentResponse>> GetByTicketIdAsync(
        int ticketId, string requesterId, string requesterRole, CancellationToken ct = default)
    {
        await AuthorizeTicketAccessAsync(ticketId, requesterId, requesterRole, ct);

        var comments = await _db.Comments
            .AsNoTracking()
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var authorNames = new Dictionary<string, string>();
        foreach (var id in authorIds)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is not null) authorNames[id] = user.FullName;
        }

        return comments.Select(c => new CommentResponse
        {
            Id = c.Id,
            TicketId = c.TicketId,
            AuthorId = c.AuthorId,
            AuthorName = authorNames.GetValueOrDefault(c.AuthorId),
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList().AsReadOnly();
    }

    public async Task<CommentResponse> AddCommentAsync(
        int ticketId, string authorId, string authorRole,
        CreateCommentRequest request, CancellationToken ct = default)
    {
        await AuthorizeTicketAccessAsync(ticketId, authorId, authorRole, ct);

        var comment = new Comment
        {
            TicketId = ticketId,
            AuthorId = authorId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var author = await _userManager.FindByIdAsync(authorId);

        return new CommentResponse
        {
            Id = comment.Id,
            TicketId = comment.TicketId,
            AuthorId = comment.AuthorId,
            AuthorName = author?.FullName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }

    private async Task AuthorizeTicketAccessAsync(
        int ticketId, string userId, string role, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        bool authorized = role switch
        {
            "Admin" => true,
            "Agent" => ticket.AssignedAgentId == userId,
            "Employee" => ticket.EmployeeId == userId,
            _ => false
        };

        if (!authorized)
            throw new UnauthorizedAccessException("You do not have access to this ticket.");
    }
}
