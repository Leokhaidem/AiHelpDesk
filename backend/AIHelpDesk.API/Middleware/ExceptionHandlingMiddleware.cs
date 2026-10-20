using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AIHelpDesk.API.Middleware;

/// <summary>
/// Global exception handler. Converts known business exceptions to proper HTTP responses
/// and catches unexpected exceptions returning a generic 500 with a ProblemDetails body.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found"),
            InvalidOperationException => (HttpStatusCode.Conflict, "Business Rule Violation"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access Denied"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid Argument"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception.");
        else
            _logger.LogWarning(ex, "Business exception: {Message}", ex.Message);

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
