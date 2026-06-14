using FleetIQ.Notifications.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Notifications.UnitTests;

public class NotificationTests
{
    private static Notification NewAggregate() => Notification.Create("Recipient", "Channel", "TemplateId");

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(NotificationStatus.Pending);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void MarkSent_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.MarkSent();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(NotificationStatus.Sent);
    }

    [Fact]
    public void MarkSent_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.MarkSent();
        var second = entity.MarkSent();
        second.IsFailure.ShouldBeTrue();
    }
}
