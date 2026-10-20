using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Application.Priorities.DTOs;
using AIHelpDesk.Application.Priorities.Services;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Services;

/// <summary>
/// Manages priorities. Invalidates the lookup cache after every write.
/// </summary>
public sealed class PriorityService : IPriorityService
{
    private readonly AppDbContext _db;
    private readonly ILookupCache _cache;

    public PriorityService(AppDbContext db, ILookupCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<PriorityResponse>> GetAllAsync(
        bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _db.Priorities.AsNoTracking();
        if (activeOnly) query = query.Where(p => p.IsActive);

        var priorities = await query
            .OrderBy(p => p.Level)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);

        return priorities.AsReadOnly();
    }

    public async Task<PriorityResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.Priorities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? null : ToResponse(p);
    }

    public async Task<PriorityResponse> CreateAsync(
        CreatePriorityRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await _db.Priorities.AnyAsync(p => p.Name.ToLower() == name.ToLower(), ct))
            throw new InvalidOperationException($"A priority with the name '{name}' already exists.");

        if (await _db.Priorities.AnyAsync(p => p.Level == request.Level, ct))
            throw new InvalidOperationException($"A priority with level {request.Level} already exists.");

        var priority = new Priority
        {
            Name = name,
            Level = request.Level,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Priorities.Add(priority);
        await _db.SaveChangesAsync(ct);
        _cache.InvalidatePriorities();

        return ToResponse(priority);
    }

    public async Task<PriorityResponse> UpdateAsync(
        int id, UpdatePriorityRequest request, CancellationToken ct = default)
    {
        var priority = await _db.Priorities.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Priority {id} not found.");

        var name = request.Name.Trim();

        if (await _db.Priorities.AnyAsync(p => p.Id != id && p.Name.ToLower() == name.ToLower(), ct))
            throw new InvalidOperationException($"Another priority already uses the name '{name}'.");

        if (await _db.Priorities.AnyAsync(p => p.Id != id && p.Level == request.Level, ct))
            throw new InvalidOperationException($"Another priority already uses level {request.Level}.");

        priority.Name = name;
        priority.Level = request.Level;
        priority.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _cache.InvalidatePriorities();

        return ToResponse(priority);
    }

    public async Task<PriorityResponse> SetStatusAsync(
        int id, bool isActive, CancellationToken ct = default)
    {
        var priority = await _db.Priorities.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Priority {id} not found.");

        priority.IsActive = isActive;
        priority.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _cache.InvalidatePriorities();

        return ToResponse(priority);
    }

    private static PriorityResponse ToResponse(Priority p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Level = p.Level,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
