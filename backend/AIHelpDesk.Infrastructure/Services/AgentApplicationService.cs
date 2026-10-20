using AIHelpDesk.Application.AgentApplications.DTOs;
using AIHelpDesk.Application.AgentApplications.Services;
using AIHelpDesk.Application.AgentAssignment.Services;
using AIHelpDesk.Application.Tickets.DTOs;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Services;

public sealed class AgentApplicationService : IAgentApplicationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAgentAssignmentService _assignment;

    public AgentApplicationService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IAgentAssignmentService assignment)
    {
        _db = db;
        _userManager = userManager;
        _assignment = assignment;
    }

    public async Task<IReadOnlyList<int>> ValidateCategoriesAsync(
        IEnumerable<int>? categoryIds, CancellationToken ct = default)
    {
        var ids = (categoryIds ?? Enumerable.Empty<int>()).Distinct().ToList();

        if (ids.Count == 0)
            throw new ArgumentException("Select at least one category.");

        var activeCount = await _db.Categories
            .CountAsync(c => ids.Contains(c.Id) && c.IsActive, ct);

        if (activeCount != ids.Count)
            throw new ArgumentException(
                "One or more selected categories do not exist or are inactive.");

        return ids;
    }

    public async Task<AgentApplicationResponse> CreateAsync(
        string userId, CreateAgentApplicationRequest request, CancellationToken ct = default)
    {
        var ids = await ValidateCategoriesAsync(request.CategoryIds, ct);

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!await _userManager.IsInRoleAsync(user, "Employee"))
            throw new InvalidOperationException("Only Employees can apply to become an Agent.");

        var hasPending = await _db.AgentApplications.AnyAsync(
            a => a.UserId == userId && a.Status == AgentApplicationStatus.Pending, ct);

        if (hasPending)
            throw new InvalidOperationException("You already have a pending agent application.");

        var application = new AgentApplication
        {
            UserId = userId,
            Message = request.Message?.Trim(),
            Status = AgentApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Categories = ids
                .Select(id => new AgentApplicationCategory { CategoryId = id })
                .ToList()
        };

        _db.AgentApplications.Add(application);
        await _db.SaveChangesAsync(ct);

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new CategoryRefResponse { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);

        return new AgentApplicationResponse
        {
            Id = application.Id,
            UserId = application.UserId,
            Status = application.Status.ToString(),
            Message = application.Message,
            Categories = categories,
            CreatedAt = application.CreatedAt
        };
    }

    public async Task<AgentApplicationResponse?> GetMyApplicationAsync(string userId, CancellationToken ct = default)
    {
        var application = await _db.AgentApplications
            .Include(a => a.Categories)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        if (application is null)
            return null;

        return new AgentApplicationResponse
        {
            Id = application.Id,
            UserId = application.UserId,
            Status = application.Status.ToString(),
            Message = application.Message,
            Categories = await GetCategoryRefsAsync(application.Categories.Select(ac => ac.CategoryId), ct),
            CreatedAt = application.CreatedAt,
            ReviewedAt = application.ReviewedAt,
            ReviewNote = application.ReviewNote
        };
    }

    public async Task WithdrawAsync(string userId, CancellationToken ct = default)
    {
        var application = await _db.AgentApplications
            .FirstOrDefaultAsync(a => a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("No agent application found for this user.");

        if (application.Status != AgentApplicationStatus.Pending)
            throw new InvalidOperationException("Only a pending application can be withdrawn.");

        application.Status = AgentApplicationStatus.Withdrawn;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AdminAgentApplicationResponse>> GetApplicationsAsync(
        string? status, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<AgentApplication> query = _db.AgentApplications
            .AsNoTracking()
            .Include(a => a.Categories);

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<AgentApplicationStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(a => a.Status == parsed);
        }

        var totalCount = await query.CountAsync(ct);

        var applications = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AdminAgentApplicationResponse>
        {
            Items = await ToAdminResponsesAsync(applications, ct),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminAgentApplicationResponse?> GetApplicationAsync(int id, CancellationToken ct = default)
    {
        var application = await _db.AgentApplications
            .AsNoTracking()
            .Include(a => a.Categories)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (application is null)
            return null;

        return await ToAdminResponseAsync(application, ct);
    }

    public async Task<AdminAgentApplicationResponse> ApproveAsync(
        int id, ApproveAgentApplicationRequest? request, string adminId, CancellationToken ct = default)
    {
        var application = await _db.AgentApplications
            .Include(a => a.Categories)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Agent application {id} not found.");

        if (application.Status != AgentApplicationStatus.Pending)
            throw new InvalidOperationException("Only a pending application can be approved.");

        var user = await _userManager.FindByIdAsync(application.UserId)
            ?? throw new KeyNotFoundException("The applicant no longer exists.");

        // The Admin may override the requested categories; otherwise keep them.
        var requestedIds = application.Categories.Select(ac => ac.CategoryId).Distinct().ToList();
        var categoryIds = await ValidateCategoriesAsync(
            request?.CategoryIds is { Count: > 0 } ? request.CategoryIds : requestedIds, ct);

        // Flow B — role swap, profile + category links and the review state
        // are persisted together, then the sweep picks up waiting tickets.
        var rolesToRemove = (await _userManager.GetRolesAsync(user))
            .Where(role => role is "Employee" or "Agent")
            .ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException("Could not remove the applicant's current role.");
        }

        var addResult = await _userManager.AddToRoleAsync(user, "Agent");
        if (!addResult.Succeeded)
            throw new InvalidOperationException("Could not assign the Agent role.");

        var profile = await _db.AgentProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        if (profile is null)
            _db.AgentProfiles.Add(new AgentProfile { UserId = user.Id });

        var existingLinks = await _db.AgentCategories
            .Where(ac => ac.AgentId == user.Id)
            .ToListAsync(ct);
        var existingIds = existingLinks.Select(link => link.CategoryId).ToHashSet();
        var categoryIdSet = categoryIds.ToHashSet();

        // Replace by diff: re-adding a key that is still tracked (Deleted)
        // would make EF throw an identity-map conflict.
        _db.AgentCategories.RemoveRange(
            existingLinks.Where(link => !categoryIdSet.Contains(link.CategoryId)).ToList());
        _db.AgentCategories.AddRange(
            categoryIds
                .Where(categoryId => !existingIds.Contains(categoryId))
                .Select(categoryId => new AgentCategory { AgentId = user.Id, CategoryId = categoryId }));

        application.Status = AgentApplicationStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedByAdminId = adminId;
        application.ReviewNote = request?.Note?.Trim();

        await _db.SaveChangesAsync(ct);

        // The new agent may take waiting tickets in their categories right away.
        await _assignment.SweepAsync(ct: ct);

        return await ToAdminResponseAsync(application, ct);
    }

    public async Task<AdminAgentApplicationResponse> RejectAsync(
        int id, RejectAgentApplicationRequest? request, string adminId, CancellationToken ct = default)
    {
        var application = await _db.AgentApplications
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Agent application {id} not found.");

        if (application.Status != AgentApplicationStatus.Pending)
            throw new InvalidOperationException("Only a pending application can be rejected.");

        application.Status = AgentApplicationStatus.Rejected;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedByAdminId = adminId;
        application.ReviewNote = request?.Note?.Trim();

        await _db.SaveChangesAsync(ct);

        return await ToAdminResponseAsync(application, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<CategoryRefResponse>> GetCategoryRefsAsync(
        IEnumerable<int> categoryIds, CancellationToken ct)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new CategoryRefResponse { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);
    }

    private async Task<AdminAgentApplicationResponse> ToAdminResponseAsync(
        AgentApplication application, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == application.UserId, ct);

        return new AdminAgentApplicationResponse
        {
            Id = application.Id,
            UserId = application.UserId,
            ApplicantName = user?.FullName ?? string.Empty,
            ApplicantEmail = user?.Email ?? string.Empty,
            Status = application.Status.ToString(),
            Message = application.Message,
            Categories = await GetCategoryRefsAsync(
                application.Categories.Select(ac => ac.CategoryId), ct),
            CreatedAt = application.CreatedAt,
            ReviewedAt = application.ReviewedAt,
            ReviewNote = application.ReviewNote
        };
    }

    private async Task<IReadOnlyList<AdminAgentApplicationResponse>> ToAdminResponsesAsync(
        IEnumerable<AgentApplication> applications, CancellationToken ct)
    {
        var result = new List<AdminAgentApplicationResponse>();

        foreach (var application in applications)
            result.Add(await ToAdminResponseAsync(application, ct));

        return result;
    }
}