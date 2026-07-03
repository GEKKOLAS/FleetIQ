using FleetIQ.Notifications.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Notifications.UnitTests;

public class NotificationTests
{
    private static Notification NewAggregate() => Notification.Create("ops@acme.io", "Email", "shipment-delayed").Value;

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(NotificationStatus.Pending);
        entity.AttemptCount.ShouldBe(0);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_NormalizesChannelCasing()
    {
        Notification.Create("x@y.io", "email", "t").Value.Channel.ShouldBe("Email");
    }

    [Fact]
    public void Create_WithUnknownChannel_Fails()
    {
        var result = Notification.Create("x@y.io", "Fax", "t");
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(NotificationErrors.ChannelInvalid);
    }

    [Fact]
    public void MarkSent_StampsTimestamps_AndCountsAttempt()
    {
        var entity = NewAggregate();
        entity.MarkSent().IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(NotificationStatus.Sent);
        entity.AttemptCount.ShouldBe(1);
        entity.SentOnUtc.ShouldNotBeNull();
        entity.MarkSent().IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void MarkFailed_RequiresReason_AndKeepsIt()
    {
        var entity = NewAggregate();
        entity.MarkFailed(" ").IsFailure.ShouldBeTrue();
        entity.MarkFailed("SMTP timeout").IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(NotificationStatus.Failed);
        entity.FailedReason.ShouldBe("SMTP timeout");
        entity.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public void Retry_ReturnsToPending_AndClearsReason()
    {
        var entity = NewAggregate();
        entity.MarkFailed("SMTP timeout");
        entity.Retry().IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(NotificationStatus.Pending);
        entity.FailedReason.ShouldBeNull();
    }

    [Fact]
    public void Retry_AfterMaxAttempts_Fails()
    {
        var entity = NewAggregate();
        for (var i = 0; i < Notification.MaxAttempts; i++)
        {
            entity.MarkFailed($"attempt {i + 1}").IsSuccess.ShouldBeTrue();
            if (i < Notification.MaxAttempts - 1) entity.Retry().IsSuccess.ShouldBeTrue();
        }

        var final = entity.Retry();
        final.IsFailure.ShouldBeTrue();
        final.Error.ShouldBe(NotificationErrors.MaxAttemptsReached);
    }

    [Fact]
    public void Retry_WhenNotFailed_Fails()
    {
        NewAggregate().Retry().IsFailure.ShouldBeTrue();
    }
}
