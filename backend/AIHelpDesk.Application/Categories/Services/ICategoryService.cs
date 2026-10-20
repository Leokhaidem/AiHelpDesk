using AIHelpDesk.Application.Categories.DTOs;

namespace AIHelpDesk.Application.Categories.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<CategoryResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse> SetStatusAsync(int id, bool isActive, CancellationToken ct = default);
}
