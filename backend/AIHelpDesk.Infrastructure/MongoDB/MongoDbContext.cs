using AIHelpDesk.Application.Common.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace AIHelpDesk.Infrastructure.MongoDB;

/// <summary>
/// Provides a typed MongoDB database instance, configured from settings.
/// </summary>
public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<TicketHistoryDocument> TicketHistory =>
        _database.GetCollection<TicketHistoryDocument>("ticket_history");

    public IMongoCollection<AiAnalysisDocument> AiAnalysis =>
        _database.GetCollection<AiAnalysisDocument>("ai_analysis");
}

// ─── MongoDB Document Models ───────────────────────────────────────────────────

[BsonIgnoreExtraElements]
public sealed class TicketHistoryDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("ticketId")]
    public int TicketId { get; set; }

    [BsonElement("eventType")]
    public int EventType { get; set; }

    [BsonElement("oldValue")]
    public string? OldValue { get; set; }

    [BsonElement("newValue")]
    public string? NewValue { get; set; }

    [BsonElement("actorId")]
    public string? ActorId { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("details")]
    public string? Details { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class AiAnalysisDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("ticketId")]
    public int TicketId { get; set; }

    [BsonElement("success")]
    public bool Success { get; set; }

    [BsonElement("rawResponse")]
    public string? RawResponse { get; set; }

    [BsonElement("parsedCategory")]
    public string? ParsedCategory { get; set; }

    [BsonElement("parsedPriority")]
    public string? ParsedPriority { get; set; }

    [BsonElement("parsedSummary")]
    public string? ParsedSummary { get; set; }

    [BsonElement("parsedSuggestedResolution")]
    public string? ParsedSuggestedResolution { get; set; }

    [BsonElement("failureReason")]
    public string? FailureReason { get; set; }

    [BsonElement("attemptNumber")]
    public int AttemptNumber { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
}
