using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Notifications.Domain;

// NOTE: Consumes domain events from every service; add MailKit/Twilio/Firebase providers and template rendering (Scriban) later.
public enum NotificationStatus { Pending = 0, Sent = 1, Failed = 2 }

public static class NotificationErrors
{
    public static readonly Error InvalidTransition =
        new("Notification.InvalidTransition", "The item is not in a state that allows this operation.");
    public static readonly Error RecipientRequired =
        new("Notification.RecipientRequired", "A recipient is required.");
    public static readonly Error ChannelInvalid =
        new("Notification.ChannelInvalid", "Channel must be one of: Email, Sms, Push.");
    public static readonly Error TemplateRequired =
        new("Notification.TemplateRequired", "A template id is required.");
    public static readonly Error ReasonRequired =
        new("Notification.ReasonRequired", "A failure reason is required.");
    public static readonly Error MaxAttemptsReached =
        new("Notification.MaxAttemptsReached", "The notification reached the maximum number of delivery attempts.");
}

/// <summary>Notification aggregate root. Lifecycle: Pending -> Sent | Failed; a failed
/// notification can be retried (back to Pending) until MaxAttempts is reached.</summary>
public sealed class Notification : AggregateRoot<Guid>
{
    public const int MaxAttempts = 5;
    private static readonly string[] AllowedChannels = ["Email", "Sms", "Push"];

    public string Recipient { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public string TemplateId { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? SentOnUtc { get; private set; }
    public DateTime? LastAttemptOnUtc { get; private set; }
    public string? FailedReason { get; private set; }

    private Notification() { } // EF

    private Notification(Guid id, string recipient, string channel, string templateId) : base(id)
    {
        Recipient = recipient;
        Channel = channel;
        TemplateId = templateId;
        Status = NotificationStatus.Pending;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<Notification> Create(string recipient, string channel, string templateId)
    {
        if (string.IsNullOrWhiteSpace(recipient)) return Result.Failure<Notification>(NotificationErrors.RecipientRequired);
        var normalizedChannel = AllowedChannels.FirstOrDefault(c => string.Equals(c, channel, StringComparison.OrdinalIgnoreCase));
        if (normalizedChannel is null) return Result.Failure<Notification>(NotificationErrors.ChannelInvalid);
        if (string.IsNullOrWhiteSpace(templateId)) return Result.Failure<Notification>(NotificationErrors.TemplateRequired);

        var entity = new Notification(Guid.NewGuid(), recipient.Trim(), normalizedChannel, templateId.Trim());
        entity.Raise(new Events.NotificationCreatedDomainEvent(entity.Id));
        return Result.Success(entity);
    }

    public Result MarkSent()
    {
        if (Status != NotificationStatus.Pending) return Result.Failure(NotificationErrors.InvalidTransition);

        Status = NotificationStatus.Sent;
        AttemptCount++;
        LastAttemptOnUtc = SentOnUtc = DateTime.UtcNow;
        FailedReason = null;
        Raise(new Events.NotificationStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result MarkFailed(string reason)
    {
        if (Status != NotificationStatus.Pending) return Result.Failure(NotificationErrors.InvalidTransition);
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(NotificationErrors.ReasonRequired);

        Status = NotificationStatus.Failed;
        AttemptCount++;
        LastAttemptOnUtc = DateTime.UtcNow;
        FailedReason = reason.Trim();
        Raise(new Events.NotificationFailedDomainEvent(Id, FailedReason, AttemptCount));
        return Result.Success();
    }

    public Result Retry()
    {
        if (Status != NotificationStatus.Failed) return Result.Failure(NotificationErrors.InvalidTransition);
        if (AttemptCount >= MaxAttempts) return Result.Failure(NotificationErrors.MaxAttemptsReached);

        Status = NotificationStatus.Pending;
        FailedReason = null;
        Raise(new Events.NotificationStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
