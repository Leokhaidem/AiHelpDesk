using AIHelpDesk.Application.Categories.DTOs;
using AIHelpDesk.Application.Categories.Services;
using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Services;

/// <summary>
/// Manages categories. Each category response includes the agents who work it
/// (resolved from AgentCategory links). Invalidates the lookup cache after every write.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly ILookupCache _cache;

    public CategoryService(
        AppDbContext db,
        ILookupCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _db.Categories.AsNoTracking();
        if (activeOnly) query = query.Where(c => c.IsActive);

        var categories = await query
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return (await ToResponsesAsync(categories, ct)).ToList().AsReadOnly();
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null) return null;

        return (await ToResponsesAsync([category], ct)).Single();
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await _db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct))
            throw new InvalidOperationException($"A category with the name '{name}' already exists.");

        var category = new Category
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);

        // Invalidate only after successful save
        _cache.InvalidateCategories();

        return await GetByIdAsync(category.Id, ct)
               ?? throw new InvalidOperationException("Category was created but could not be retrieved.");
    }

    public async Task<CategoryResponse> UpdateAsync(
        int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        var name = request.Name.Trim();

        if (await _db.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower(), ct))
            throw new InvalidOperationException($"Another category already uses the name '{name}'.");

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _cache.InvalidateCategories();

        return await GetByIdAsync(id, ct)!;
    }

    public async Task<CategoryResponse> SetStatusAsync(
        int id, bool isActive, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _cache.InvalidateCategories();

        return await GetByIdAsync(id, ct)!;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<List<CategoryResponse>> ToResponsesAsync(
        IReadOnlyList<Category> categories, CancellationToken ct)
    {
        var categoryIds = categories.Select(c => c.Id).ToList();

        var links = await _db.AgentCategories
            .AsNoTracking()
            .Where(ac => categoryIds.Contains(ac.CategoryId))
            .ToListAsync(ct);

        var agentIds = links.Select(l => l.AgentId).Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => agentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var agentsByCategory = links
            .Where(l => users.ContainsKey(l.AgentId))
            .GroupBy(l => l.CategoryId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(l => new CategoryAgentRef
                    {
                        Id = l.AgentId,
                        FullName = users[l.AgentId].FullName
                    })
                    .OrderBy(a => a.FullName)
                    .ToList());

        return categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive,
            Agents = agentsByCategory.GetValueOrDefault(c.Id) ?? [],
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
    }
}
