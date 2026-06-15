using FleetIQ.Notifications.Domain;

namespace FleetIQ.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification entity, CancellationToken ct);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Notification>> ListRecentAsync(int take, CancellationToken ct);
}
