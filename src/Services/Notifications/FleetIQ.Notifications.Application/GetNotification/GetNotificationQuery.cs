using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Notifications.Application.Abstractions;
using FleetIQ.Notifications.Application.ListNotifications;

namespace FleetIQ.Notifications.Application.GetNotification;

public sealed record GetNotificationQuery(Guid Id) : IQuery<NotificationResponse?>;

public sealed class GetNotificationQueryHandler(INotificationRepository repository)
    : IQueryHandler<GetNotificationQuery, NotificationResponse?>
{
    public async Task<NotificationResponse?> Handle(GetNotificationQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null
            ? null
            : new NotificationResponse(
                entity.Id, entity.Recipient, entity.Channel, entity.TemplateId, entity.Status.ToString(), entity.CreatedOnUtc,
                entity.AttemptCount, entity.SentOnUtc, entity.FailedReason);
    }
}
