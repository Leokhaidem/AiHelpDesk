using AIHelpDesk.Application.Priorities.DTOs;

namespace AIHelpDesk.Application.Priorities.Services;

public interface IPriorityService
{
    Task<IReadOnlyList<PriorityResponse>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<PriorityResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PriorityResponse> CreateAsync(CreatePriorityRequest request, CancellationToken ct = default);
    Task<PriorityResponse> UpdateAsync(int id, UpdatePriorityRequest request, CancellationToken ct = default);
    Task<PriorityResponse> SetStatusAsync(int id, bool isActive, CancellationToken ct = default);
}
