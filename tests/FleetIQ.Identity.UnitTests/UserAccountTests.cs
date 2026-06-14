using FleetIQ.Identity.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Identity.UnitTests;

public class UserAccountTests
{
    private static UserAccount NewAggregate() => UserAccount.Create("Email", "FullName", Guid.NewGuid());

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(UserAccountStatus.Pending);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Activate_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Activate();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(UserAccountStatus.Active);
    }

    [Fact]
    public void Activate_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Activate();
        var second = entity.Activate();
        second.IsFailure.ShouldBeTrue();
    }
}
