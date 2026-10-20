namespace AIHelpDesk.Application.Common.Interfaces;

/// <summary>
/// Represents an AI analysis attempt stored in MongoDB.
/// </summary>
public sealed class AiAnalysisRecord
{
    public string? Id { get; set; } // MongoDB ObjectId
    public int TicketId { get; set; }
    public bool Success { get; set; }
    public string? RawResponse { get; set; }
    public string? ParsedCategory { get; set; }
    public string? ParsedPriority { get; set; }
    public string? ParsedSummary { get; set; }
    public string? ParsedSuggestedResolution { get; set; }
    public string? FailureReason { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Repository for AI analysis records stored in MongoDB.
/// </summary>
public interface IAiAnalysisRepository
{
    Task AddAsync(AiAnalysisRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<AiAnalysisRecord>> GetByTicketIdAsync(int ticketId, CancellationToken ct = default);
    Task<int> GetAttemptCountAsync(int ticketId, CancellationToken ct = default);
}
