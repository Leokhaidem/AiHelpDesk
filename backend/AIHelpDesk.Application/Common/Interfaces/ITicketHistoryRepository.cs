using AIHelpDesk.Domain.Enums;

namespace AIHelpDesk.Application.Common.Interfaces;

/// <summary>
/// Represents a single ticket history / audit event stored in MongoDB.
/// </summary>
public sealed class TicketHistoryEvent
{
    public string? Id { get; set; } // MongoDB ObjectId as string
    public int TicketId { get; set; }
    public TicketEventType EventType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ActorId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }
}

/// <summary>
/// Repository for ticket audit history stored in MongoDB.
/// </summary>
public interface ITicketHistoryRepository
{
    Task AddAsync(TicketHistoryEvent historyEvent, CancellationToken ct = default);
    Task<IReadOnlyList<TicketHistoryEvent>> GetByTicketIdAsync(int ticketId, CancellationToken ct = default);
}
