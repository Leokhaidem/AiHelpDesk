using System.Threading.Channels;
using AIHelpDesk.Application.AgentAssignment.Services;
using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIHelpDesk.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background service that processes AI analysis jobs for newly created tickets.
/// Consumes ticket IDs from a <see cref="Channel{T}"/>, runs Gemini analysis,
/// validates the result, auto-assigns the appropriate agent, and updates the ticket status.
/// </summary>
public sealed class AiAnalysisWorker : BackgroundService
{
    private readonly Channel<int> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiAnalysisWorker> _logger;

    public AiAnalysisWorker(
        Channel<int> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<AiAnalysisWorker> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AiAnalysisWorker started.");

        await foreach (var ticketId in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessTicketAsync(ticketId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error processing AI analysis for ticket {TicketId}.", ticketId);
            }
        }

        _logger.LogInformation("AiAnalysisWorker stopped.");
    }

    private async Task ProcessTicketAsync(int ticketId, CancellationToken ct)
    {
        _logger.LogInformation("Starting AI analysis for ticket {TicketId}.", ticketId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiAnalysisService>();
        var lookupCache = scope.ServiceProvider.GetRequiredService<ILookupCache>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<ITicketHistoryRepository>();
        var analysisRepo = scope.ServiceProvider.GetRequiredService<IAiAnalysisRepository>();

        var ticket = await db.Tickets
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
        {
            _logger.LogWarning("Ticket {TicketId} not found when processing AI analysis.", ticketId);
            return;
        }

        // Transition to AnalysisInProgress
        ticket.Status = TicketStatus.AnalysisInProgress;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await historyRepo.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.StatusChanged,
            OldValue = TicketStatus.Created.ToString(),
            NewValue = TicketStatus.AnalysisInProgress.ToString(),
            ActorId = "system",
            Timestamp = DateTime.UtcNow,
            Details = "AI analysis started."
        }, ct);

        // Get active categories and priorities from cache
        var categories = await lookupCache.GetCategoriesAsync(ct);
        var priorities = await lookupCache.GetPrioritiesAsync(ct);

        var attemptNumber = await analysisRepo.GetAttemptCountAsync(ticketId, ct) + 1;

        // Call AI
        var result = await aiService.AnalyzeAsync(ticket.Description, categories, priorities, ct);

        var analysisRecord = new AiAnalysisRecord
        {
            TicketId = ticketId,
            Success = result.Success,
            RawResponse = result.RawResponse,
            ParsedCategory = result.Category,
            ParsedPriority = result.Priority,
            ParsedSummary = result.Summary,
            ParsedSuggestedResolution = result.SuggestedResolution,
            FailureReason = result.FailureReason,
            AttemptNumber = attemptNumber,
            Timestamp = DateTime.UtcNow
        };
        await analysisRepo.AddAsync(analysisRecord, ct);

        if (!result.Success)
        {
            _logger.LogWarning(
                "AI analysis failed for ticket {TicketId}: {Reason}", ticketId, result.FailureReason);

            ticket.Status = TicketStatus.AnalysisFailed;
            ticket.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await historyRepo.AddAsync(new TicketHistoryEvent
            {
                TicketId = ticketId,
                EventType = TicketEventType.AiAnalysisFailed,
                OldValue = TicketStatus.AnalysisInProgress.ToString(),
                NewValue = TicketStatus.AnalysisFailed.ToString(),
                ActorId = "system",
                Timestamp = DateTime.UtcNow,
                Details = result.FailureReason
            }, ct);
            return;
        }

        // AI succeeded — find category and priority IDs
        var category = await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Name.ToLower() == result.Category!.ToLower() && c.IsActive, ct);

        var priority = await db.Priorities
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Name.ToLower() == result.Priority!.ToLower() && p.IsActive, ct);

        if (category is null || priority is null)
        {
            _logger.LogWarning(
                "AI returned valid names but DB lookup failed for ticket {TicketId}. Category: {Cat}, Priority: {Pri}",
                ticketId, result.Category, result.Priority);

            ticket.Status = TicketStatus.AnalysisFailed;
            ticket.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return;
        }

        // Apply AI results
        ticket.CategoryId = category.Id;
        ticket.PriorityId = priority.Id;
        ticket.AiSummary = result.Summary;
        ticket.AiSuggestedResolution = result.SuggestedResolution;
        ticket.UpdatedAt = DateTime.UtcNow;

        await historyRepo.AddAsync(new TicketHistoryEvent
        {
            TicketId = ticketId,
            EventType = TicketEventType.AiAnalysisSucceeded,
            NewValue = $"Category: {result.Category}, Priority: {result.Priority}",
            ActorId = "system",
            Timestamp = DateTime.UtcNow
        }, ct);

        // Flow F: hand the analyzed ticket to the assignment algorithm
        // (least-loaded agent linked to the category who is active, available
        // and under capacity). On success the service saves the ticket and
        // writes the AgentAssigned event itself.
        var assignmentService = scope.ServiceProvider.GetRequiredService<IAgentAssignmentService>();
        var assignedAgentId = await assignmentService.TryAssignTicketAsync(ticketId, ct: ct);

        if (assignedAgentId is null)
        {
            // No eligible agent right now: park the ticket in
            // AwaitingAssignment. The next assignment sweep (an agent becomes
            // available, categories change, ...) or an Admin picks it up.
            ticket.Status = TicketStatus.AwaitingAssignment;
            ticket.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await historyRepo.AddAsync(new TicketHistoryEvent
            {
                TicketId = ticketId,
                EventType = TicketEventType.StatusChanged,
                OldValue = TicketStatus.AnalysisInProgress.ToString(),
                NewValue = TicketStatus.AwaitingAssignment.ToString(),
                ActorId = "system",
                Timestamp = DateTime.UtcNow,
                Details = "No eligible agent available. An admin must assign one."
            }, ct);

            _logger.LogInformation(
                "Ticket {TicketId} analyzed but no eligible agent is available. Waiting for assignment.",
                ticketId);
        }

        _logger.LogInformation("AI analysis complete for ticket {TicketId}. Status: {Status}.",
            ticketId, ticket.Status);
    }
}
