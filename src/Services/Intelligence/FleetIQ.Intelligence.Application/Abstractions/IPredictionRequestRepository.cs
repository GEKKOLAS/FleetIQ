using FleetIQ.Intelligence.Domain;

namespace FleetIQ.Intelligence.Application.Abstractions;

public interface IPredictionRequestRepository
{
    Task AddAsync(PredictionRequest entity, CancellationToken ct);
    Task<PredictionRequest?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PredictionRequest>> ListRecentAsync(int take, CancellationToken ct);
}
