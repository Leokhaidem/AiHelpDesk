namespace AIHelpDesk.Application.Common.Interfaces;

/// <summary>
/// Cache-aside cache for active categories and priorities.
/// Implemented in Infrastructure using IMemoryCache with SemaphoreSlim for stampede protection.
/// </summary>
public interface ILookupCache
{
    /// <summary>Returns names of all active categories. Loads from DB on cache miss.</summary>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>Returns names of all active priorities. Loads from DB on cache miss.</summary>
    Task<IReadOnlyList<string>> GetPrioritiesAsync(CancellationToken ct = default);

    /// <summary>Removes categories from cache. Call after any category write (create/update/deactivate).</summary>
    void InvalidateCategories();

    /// <summary>Removes priorities from cache. Call after any priority write.</summary>
    void InvalidatePriorities();
}
