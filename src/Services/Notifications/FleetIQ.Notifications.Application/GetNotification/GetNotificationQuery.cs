using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Notifications.Application.Abstractions;

namespace FleetIQ.Notifications.Application.GetNotification;

public sealed record GetNotificationQuery(Guid Id) : IQuery<NotificationResponse?>;

public sealed record NotificationResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetNotificationQueryHandler(INotificationRepository repository)
    : IQueryHandler<GetNotificationQuery, NotificationResponse?>
{
    public async Task<NotificationResponse?> Handle(GetNotificationQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new NotificationResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
