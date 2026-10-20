using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace AIHelpDesk.Infrastructure.Cache;

/// <summary>
/// Cache-aside implementation of <see cref="ILookupCache"/> using <see cref="IMemoryCache"/>.
/// Uses <see cref="SemaphoreSlim"/> with double-checked locking to prevent cache stampedes.
/// Resolves <see cref="AppDbContext"/> through <see cref="IServiceScopeFactory"/> because
/// this class is registered as a singleton but DbContext is scoped.
/// </summary>
public sealed class LookupCache : ILookupCache
{
    private const string CategoriesKey = "lookup_cache:categories";
    private const string PrioritiesKey = "lookup_cache:priorities";
    private static readonly TimeSpan Expiry = TimeSpan.FromHours(6);

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly SemaphoreSlim _categorySemaphore = new(1, 1);
    private readonly SemaphoreSlim _prioritySemaphore = new(1, 1);

    public LookupCache(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CategoriesKey, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        await _categorySemaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring the lock
            if (_cache.TryGetValue(CategoriesKey, out cached) && cached is not null)
                return cached;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var names = await db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync(ct);

            var result = names.AsReadOnly();
            _cache.Set(CategoriesKey, (IReadOnlyList<string>)result, Expiry);
            return result;
        }
        finally
        {
            _categorySemaphore.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetPrioritiesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(PrioritiesKey, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        await _prioritySemaphore.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(PrioritiesKey, out cached) && cached is not null)
                return cached;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var names = await db.Priorities
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Level)
                .Select(p => p.Name)
                .ToListAsync(ct);

            var result = names.AsReadOnly();
            _cache.Set(PrioritiesKey, (IReadOnlyList<string>)result, Expiry);
            return result;
        }
        finally
        {
            _prioritySemaphore.Release();
        }
    }

    public void InvalidateCategories() => _cache.Remove(CategoriesKey);

    public void InvalidatePriorities() => _cache.Remove(PrioritiesKey);
}
