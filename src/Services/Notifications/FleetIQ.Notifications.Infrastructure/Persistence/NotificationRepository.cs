using FleetIQ.Notifications.Application.Abstractions;
using FleetIQ.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Notifications.Infrastructure.Persistence;

public sealed class NotificationRepository(NotificationsDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification entity, CancellationToken ct) => await db.Notifications.AddAsync(entity, ct);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Notification>> ListRecentAsync(int take, CancellationToken ct)
    {
        take = take is < 1 or > 100 ? 20 : take;
        return await db.Notifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedOnUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
