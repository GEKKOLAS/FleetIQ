using FleetIQ.Planning.Domain;
using FleetIQ.Planning.Domain.Events;
using Shouldly;
using Xunit;

namespace FleetIQ.Planning.UnitTests;

public class LoadTests
{
    private static Load NewDraft() => Load.Create("Lisboa", "Porto").Value;

    private static Load NewPlanned()
    {
        var load = NewDraft();
        load.AddOrder(Guid.NewGuid(), "Pallet", 120m);
        load.AssignCarrier(Guid.NewGuid());
        load.AttachRoute(Guid.NewGuid());
        load.Plan().IsSuccess.ShouldBeTrue();
        return load;
    }

    [Fact]
    public void Create_SetsDraft_GeneratesReference_AndRaisesEvent()
    {
        var result = Load.Create("Lisboa", "Porto");

        result.IsSuccess.ShouldBeTrue();
        var load = result.Value;
        load.Status.ShouldBe(LoadStatus.Draft);
        load.Reference.ShouldStartWith("LD-");
        load.DomainEvents.OfType<LoadCreatedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Create_WithEmptyCity_Fails()
    {
        var result = Load.Create("", "Porto");
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(LoadErrors.CityRequired);
    }

    [Fact]
    public void AddOrder_AccumulatesTotalWeight()
    {
        var load = NewDraft();
        load.AddOrder(Guid.NewGuid(), "A", 100m);
        load.AddOrder(Guid.NewGuid(), "B", 50m);
        load.TotalWeightKg.ShouldBe(150m);
        load.Orders.Count.ShouldBe(2);
    }

    [Fact]
    public void Plan_WithoutOrders_Fails()
    {
        var load = NewDraft();
        load.AssignCarrier(Guid.NewGuid());
        load.AttachRoute(Guid.NewGuid());
        load.Plan().Error.ShouldBe(LoadErrors.NoOrders);
    }

    [Fact]
    public void Plan_WithoutCarrier_Fails()
    {
        var load = NewDraft();
        load.AddOrder(Guid.NewGuid(), "A", 10m);
        load.AttachRoute(Guid.NewGuid());
        load.Plan().Error.ShouldBe(LoadErrors.NoCarrier);
    }

    [Fact]
    public void Plan_WithoutRoute_Fails()
    {
        var load = NewDraft();
        load.AddOrder(Guid.NewGuid(), "A", 10m);
        load.AssignCarrier(Guid.NewGuid());
        load.Plan().Error.ShouldBe(LoadErrors.NoRoute);
    }

    [Fact]
    public void HappyPath_PlanThenConfirm_RaisesConfirmedEvent()
    {
        var load = NewPlanned();
        load.Status.ShouldBe(LoadStatus.Planned);

        load.Confirm().IsSuccess.ShouldBeTrue();
        load.Status.ShouldBe(LoadStatus.Confirmed);
        load.ConfirmedOnUtc.ShouldNotBeNull();
        load.DomainEvents.OfType<LoadConfirmedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Confirm_BeforePlan_Fails()
    {
        var load = NewDraft();
        load.Confirm().Error.ShouldBe(LoadErrors.NotPlanned);
    }

    [Fact]
    public void AddOrder_AfterPlan_Fails()
    {
        var load = NewPlanned();
        load.AddOrder(Guid.NewGuid(), "Late", 5m).Error.ShouldBe(LoadErrors.NotDraft);
    }

    [Fact]
    public void Cancel_FromDraft_Succeeds_ButNotAfterConfirm()
    {
        var draft = NewDraft();
        draft.Cancel().IsSuccess.ShouldBeTrue();
        draft.Status.ShouldBe(LoadStatus.Cancelled);

        var confirmed = NewPlanned();
        confirmed.Confirm();
        confirmed.Cancel().Error.ShouldBe(LoadErrors.AlreadyFinished);
    }
}
