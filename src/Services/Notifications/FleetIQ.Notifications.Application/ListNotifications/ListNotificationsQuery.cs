using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Notifications.Application.Abstractions;

namespace FleetIQ.Notifications.Application.ListNotifications;

public sealed record ListNotificationsQuery(int Take) : IQuery<IReadOnlyList<NotificationResponse>>;

public sealed record NotificationResponse(
    Guid Id, string Recipient, string Channel, string TemplateId, string Status, DateTime CreatedOnUtc);

public sealed class ListNotificationsQueryHandler(INotificationRepository repository)
    : IQueryHandler<ListNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    public async Task<IReadOnlyList<NotificationResponse>> Handle(ListNotificationsQuery query, CancellationToken ct)
    {
        var items = await repository.ListRecentAsync(query.Take, ct);
        return items
            .Select(n => new NotificationResponse(n.Id, n.Recipient, n.Channel, n.TemplateId, n.Status.ToString(), n.CreatedOnUtc))
            .ToList();
    }
}
