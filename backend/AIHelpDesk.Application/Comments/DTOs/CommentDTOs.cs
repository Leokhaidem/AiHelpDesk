namespace AIHelpDesk.Application.Comments.DTOs;

public sealed class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public sealed class CommentResponse
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
