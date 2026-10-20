using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Infrastructure.MongoDB;
using MongoDB.Driver;

namespace AIHelpDesk.Infrastructure.Repositories;

/// <summary>Stores and retrieves AI analysis attempt records in MongoDB.</summary>
public sealed class AiAnalysisRepository : IAiAnalysisRepository
{
    private readonly MongoDbContext _mongo;

    public AiAnalysisRepository(MongoDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task AddAsync(AiAnalysisRecord record, CancellationToken ct = default)
    {
        var doc = new AiAnalysisDocument
        {
            TicketId = record.TicketId,
            Success = record.Success,
            RawResponse = record.RawResponse,
            ParsedCategory = record.ParsedCategory,
            ParsedPriority = record.ParsedPriority,
            ParsedSummary = record.ParsedSummary,
            ParsedSuggestedResolution = record.ParsedSuggestedResolution,
            FailureReason = record.FailureReason,
            AttemptNumber = record.AttemptNumber,
            Timestamp = record.Timestamp
        };

        await _mongo.AiAnalysis.InsertOneAsync(doc, cancellationToken: ct);
        record.Id = doc.Id;
    }

    public async Task<IReadOnlyList<AiAnalysisRecord>> GetByTicketIdAsync(
        int ticketId, CancellationToken ct = default)
    {
        var filter = Builders<AiAnalysisDocument>.Filter.Eq(d => d.TicketId, ticketId);
        var sort = Builders<AiAnalysisDocument>.Sort.Ascending(d => d.Timestamp);

        var docs = await _mongo.AiAnalysis
            .Find(filter)
            .Sort(sort)
            .ToListAsync(ct);

        return docs.Select(d => new AiAnalysisRecord
        {
            Id = d.Id,
            TicketId = d.TicketId,
            Success = d.Success,
            RawResponse = d.RawResponse,
            ParsedCategory = d.ParsedCategory,
            ParsedPriority = d.ParsedPriority,
            ParsedSummary = d.ParsedSummary,
            ParsedSuggestedResolution = d.ParsedSuggestedResolution,
            FailureReason = d.FailureReason,
            AttemptNumber = d.AttemptNumber,
            Timestamp = d.Timestamp
        }).ToList().AsReadOnly();
    }

    public async Task<int> GetAttemptCountAsync(int ticketId, CancellationToken ct = default)
    {
        var filter = Builders<AiAnalysisDocument>.Filter.Eq(d => d.TicketId, ticketId);
        return (int)await _mongo.AiAnalysis.CountDocumentsAsync(filter, cancellationToken: ct);
    }
}
