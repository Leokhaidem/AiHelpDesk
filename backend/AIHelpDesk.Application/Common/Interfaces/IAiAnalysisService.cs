namespace AIHelpDesk.Application.Common.Interfaces;

/// <summary>
/// Result of an AI analysis call.
/// </summary>
public sealed record AiAnalysisResult(
    bool Success,
    string? Category,
    string? Priority,
    string? Summary,
    string? SuggestedResolution,
    string? FailureReason,
    string? RawResponse);

/// <summary>
/// Analyzes a ticket description using the AI model (Gemini).
/// Must not create categories or priorities — only choose from the provided lists.
/// </summary>
public interface IAiAnalysisService
{
    /// <summary>
    /// Calls the AI model with the ticket description and constrained category/priority lists.
    /// Returns a structured result. Never throws for AI-level failures — wraps them in the result.
    /// </summary>
    Task<AiAnalysisResult> AnalyzeAsync(
        string ticketDescription,
        IReadOnlyList<string> allowedCategories,
        IReadOnlyList<string> allowedPriorities,
        CancellationToken ct = default);
}
