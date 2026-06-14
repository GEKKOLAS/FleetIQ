using FleetIQ.Notifications.Application.Abstractions;
using FleetIQ.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Notifications.Infrastructure.Persistence;

public sealed class NotificationRepository(NotificationsDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification entity, CancellationToken ct) => await db.Notifications.AddAsync(entity, ct);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);
}
