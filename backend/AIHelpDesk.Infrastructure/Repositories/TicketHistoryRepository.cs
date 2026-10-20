using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.MongoDB;
using MongoDB.Driver;

namespace AIHelpDesk.Infrastructure.Repositories;

/// <summary>Stores and retrieves ticket audit events in MongoDB.</summary>
public sealed class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly MongoDbContext _mongo;

    public TicketHistoryRepository(MongoDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task AddAsync(TicketHistoryEvent historyEvent, CancellationToken ct = default)
    {
        var doc = new TicketHistoryDocument
        {
            TicketId = historyEvent.TicketId,
            EventType = (int)historyEvent.EventType,
            OldValue = historyEvent.OldValue,
            NewValue = historyEvent.NewValue,
            ActorId = historyEvent.ActorId,
            Timestamp = historyEvent.Timestamp,
            Details = historyEvent.Details
        };

        await _mongo.TicketHistory.InsertOneAsync(doc, cancellationToken: ct);
        historyEvent.Id = doc.Id;
    }

    public async Task<IReadOnlyList<TicketHistoryEvent>> GetByTicketIdAsync(
        int ticketId, CancellationToken ct = default)
    {
        var filter = Builders<TicketHistoryDocument>.Filter.Eq(d => d.TicketId, ticketId);
        var sort = Builders<TicketHistoryDocument>.Sort.Ascending(d => d.Timestamp);

        var docs = await _mongo.TicketHistory
            .Find(filter)
            .Sort(sort)
            .ToListAsync(ct);

        return docs.Select(d => new TicketHistoryEvent
        {
            Id = d.Id,
            TicketId = d.TicketId,
            EventType = (TicketEventType)d.EventType,
            OldValue = d.OldValue,
            NewValue = d.NewValue,
            ActorId = d.ActorId,
            Timestamp = d.Timestamp,
            Details = d.Details
        }).ToList().AsReadOnly();
    }
}
