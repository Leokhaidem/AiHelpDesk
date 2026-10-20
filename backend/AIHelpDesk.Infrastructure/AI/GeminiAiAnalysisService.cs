using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIHelpDesk.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIHelpDesk.Infrastructure.AI;

/// <summary>
/// Calls the Gemini REST API (configured model) with structured output to classify a ticket.
/// Enforces the hard rule: AI may not create categories or priorities beyond the provided lists.
/// Three enforcement layers:
/// 1. Prompt: explicitly lists allowed values and instructs the model to choose only from them.
/// 2. Response schema: enum constraints in the Gemini responseSchema.
/// 3. Code validation: post-parse case-insensitive check against cached lists (this file, see ParseAndValidate).
/// </summary>
public sealed class GeminiAiAnalysisService : IAiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAiAnalysisService> _logger;

    // Gemini API base URL (v1beta supports generateContent with responseSchema)
    private const string GeminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiAiAnalysisService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiAnalysisService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AiAnalysisResult> AnalyzeAsync(
        string ticketDescription,
        IReadOnlyList<string> allowedCategories,
        IReadOnlyList<string> allowedPriorities,
        CancellationToken ct = default)
    {
        if (allowedCategories.Count == 0 || allowedPriorities.Count == 0)
        {
            return new AiAnalysisResult(
                Success: false,
                Category: null,
                Priority: null,
                Summary: null,
                SuggestedResolution: null,
                FailureReason: "No active categories or priorities configured. Cannot perform AI analysis.",
                RawResponse: null);
        }

        var apiKey = _configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key is not configured.");

        var modelName = _configuration["Gemini:Model"] ?? "gemini-2.0-flash-lite";

        var prompt = BuildPrompt(ticketDescription, allowedCategories, allowedPriorities);
        var requestBody = BuildRequestBody(prompt, allowedCategories, allowedPriorities);

        var url = $"{GeminiBaseUrl}/{modelName}:generateContent?key={apiKey}";

        string rawResponse = string.Empty;
        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
            rawResponse = await httpResponse.Content.ReadAsStringAsync(ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API returned {StatusCode}: {Body}",
                    httpResponse.StatusCode, rawResponse);
                return new AiAnalysisResult(
                    Success: false,
                    Category: null, Priority: null, Summary: null, SuggestedResolution: null,
                    FailureReason: $"Gemini API error: HTTP {(int)httpResponse.StatusCode}",
                    RawResponse: rawResponse);
            }

            return ParseAndValidate(rawResponse, allowedCategories, allowedPriorities);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Gemini API call failed for ticket analysis.");
            return new AiAnalysisResult(
                Success: false,
                Category: null, Priority: null, Summary: null, SuggestedResolution: null,
                FailureReason: $"AI service error: {ex.Message}",
                RawResponse: rawResponse);
        }
    }

    private static string BuildPrompt(
        string description,
        IReadOnlyList<string> categories,
        IReadOnlyList<string> priorities)
    {
        var catList = string.Join(", ", categories.Select(c => $"\"{c}\""));
        var priList = string.Join(", ", priorities.Select(p => $"\"{p}\""));

        return $"""
            You are a support ticket classification assistant. Analyze the support ticket description below and return a JSON object with exactly these fields:
            - category: MUST be one of [{catList}]
            - priority: MUST be one of [{priList}]
            - summary: A concise 1-2 sentence summary of the problem
            - suggestedResolution: A practical suggested resolution for the support agent

            IMPORTANT: You MUST choose category and priority ONLY from the provided lists. Do not invent new values.

            Ticket Description:
            {description}
            """;
    }

    private static object BuildRequestBody(
        string prompt,
        IReadOnlyList<string> allowedCategories,
        IReadOnlyList<string> allowedPriorities)
    {
        // Use Gemini structured output with responseSchema to constrain enum values
        return new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        category = new
                        {
                            type = "string",
                            @enum = allowedCategories.ToArray()
                        },
                        priority = new
                        {
                            type = "string",
                            @enum = allowedPriorities.ToArray()
                        },
                        summary = new { type = "string" },
                        suggestedResolution = new { type = "string" }
                    },
                    required = new[] { "category", "priority", "summary", "suggestedResolution" }
                }
            }
        };
    }

    private AiAnalysisResult ParseAndValidate(
        string rawResponse,
        IReadOnlyList<string> allowedCategories,
        IReadOnlyList<string> allowedPriorities)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawResponse);
            var root = doc.RootElement;

            // Navigate Gemini response: candidates[0].content.parts[0].text
            string? jsonText = null;
            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    jsonText = parts[0].GetProperty("text").GetString();
                }
            }

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return Failure("Could not extract text from Gemini response.", rawResponse);
            }

            using var parsed = JsonDocument.Parse(jsonText);
            var result = parsed.RootElement;

            var category = result.TryGetProperty("category", out var catProp)
                ? catProp.GetString()?.Trim() : null;
            var priority = result.TryGetProperty("priority", out var priProp)
                ? priProp.GetString()?.Trim() : null;
            var summary = result.TryGetProperty("summary", out var sumProp)
                ? sumProp.GetString()?.Trim() : null;
            var suggestedResolution = result.TryGetProperty("suggestedResolution", out var resProp)
                ? resProp.GetString()?.Trim() : null;

            // Layer 3: Code-level validation (the real guarantee)
            if (string.IsNullOrWhiteSpace(category) ||
                !allowedCategories.Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase)))
            {
                return Failure(
                    $"AI returned invalid category: '{category}'. Allowed: [{string.Join(", ", allowedCategories)}]",
                    rawResponse);
            }

            if (string.IsNullOrWhiteSpace(priority) ||
                !allowedPriorities.Any(p => string.Equals(p, priority, StringComparison.OrdinalIgnoreCase)))
            {
                return Failure(
                    $"AI returned invalid priority: '{priority}'. Allowed: [{string.Join(", ", allowedPriorities)}]",
                    rawResponse);
            }

            // Normalize to exact casing from allowed lists
            var normalizedCategory = allowedCategories.First(c =>
                string.Equals(c, category, StringComparison.OrdinalIgnoreCase));
            var normalizedPriority = allowedPriorities.First(p =>
                string.Equals(p, priority, StringComparison.OrdinalIgnoreCase));

            return new AiAnalysisResult(
                Success: true,
                Category: normalizedCategory,
                Priority: normalizedPriority,
                Summary: summary ?? string.Empty,
                SuggestedResolution: suggestedResolution ?? string.Empty,
                FailureReason: null,
                RawResponse: rawResponse);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini response JSON.");
            return Failure($"JSON parse error: {ex.Message}", rawResponse);
        }
    }

    private static AiAnalysisResult Failure(string reason, string rawResponse) =>
        new(false, null, null, null, null, reason, rawResponse);
}
