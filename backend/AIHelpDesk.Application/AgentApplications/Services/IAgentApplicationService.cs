using AIHelpDesk.Application.AgentApplications.DTOs;
using AIHelpDesk.Application.Tickets.DTOs;

namespace AIHelpDesk.Application.AgentApplications.Services;

public interface IAgentApplicationService
{
    /// <summary>
    /// Checks the list is non-empty and every category exists and is active.
    /// Returns the distinct IDs. Throws ArgumentException (HTTP 400) when invalid.
    /// </summary>
    Task<IReadOnlyList<int>> ValidateCategoriesAsync(
        IEnumerable<int>? categoryIds, CancellationToken ct = default);

    /// <summary>Creates a Pending application for an Employee.</summary>
    Task<AgentApplicationResponse> CreateAsync(
        string userId, CreateAgentApplicationRequest request, CancellationToken ct = default);

    /// <summary>The user's most recent application (any status), or null when they never applied.</summary>
    Task<AgentApplicationResponse?> GetMyApplicationAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Withdraws the user's Pending application.
    /// KeyNotFoundException (404) when they have no application;
    /// InvalidOperationException (409) when it is not Pending.
    /// </summary>
    Task WithdrawAsync(string userId, CancellationToken ct = default);

    /// <summary>Admin: lists applications, newest first, optionally filtered by status name.</summary>
    Task<PagedResult<AdminAgentApplicationResponse>> GetApplicationsAsync(
        string? status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Admin: a single application, or null when not found.</summary>
    Task<AdminAgentApplicationResponse?> GetApplicationAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Admin: atomically approves a Pending application (Flow B) — swaps the
    /// Employee role for Agent, creates the AgentProfile, replaces the AgentCategory
    /// rows, marks the application Approved, then runs the assignment sweep so
    /// waiting tickets are picked up. Throws InvalidOperationException (409) when
    /// the application is not Pending.
    /// </summary>
    Task<AdminAgentApplicationResponse> ApproveAsync(
        int id, ApproveAgentApplicationRequest? request, string adminId, CancellationToken ct = default);

    /// <summary>Admin: rejects a Pending application with a note.</summary>
    Task<AdminAgentApplicationResponse> RejectAsync(
        int id, RejectAgentApplicationRequest? request, string adminId, CancellationToken ct = default);
}