using AIHelpDesk.Application.Comments.DTOs;

namespace AIHelpDesk.Application.Comments.Services;

public interface ICommentService
{
    Task<IReadOnlyList<CommentResponse>> GetByTicketIdAsync(int ticketId, string requesterId, string requesterRole, CancellationToken ct = default);
    Task<CommentResponse> AddCommentAsync(int ticketId, string authorId, string authorRole, CreateCommentRequest request, CancellationToken ct = default);
}
