using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Notifications.Domain;

// NOTE: Consumes domain events from every service; add MailKit/Twilio/Firebase providers and template rendering (Scriban).
public enum NotificationStatus { Pending = 0, Sent = 1, Failed = 2 }

public static class NotificationErrors
{
    public static readonly Error InvalidTransition =
        new("Notification.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>Notification aggregate root.</summary>
public sealed class Notification : AggregateRoot<Guid>
{
    public string Recipient { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public string TemplateId { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private Notification() { } // EF

    private Notification(Guid id, string recipient, string channel, string templateId) : base(id)
    {
        Recipient = recipient;
        Channel = channel;
        TemplateId = templateId;
        Status = NotificationStatus.Pending;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Notification Create(string recipient, string channel, string templateId)
    {
        var entity = new Notification(Guid.NewGuid(), recipient, channel, templateId);
        entity.Raise(new Events.NotificationCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result MarkSent()
    {
        if (Status != NotificationStatus.Pending) return Result.Failure(NotificationErrors.InvalidTransition);
        Status = NotificationStatus.Sent;
        Raise(new Events.NotificationStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
