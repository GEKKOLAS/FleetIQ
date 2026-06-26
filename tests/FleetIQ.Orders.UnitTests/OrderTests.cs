using FleetIQ.Orders.Domain.Orders;
using FleetIQ.Orders.Domain.Orders.Events;
using Shouldly;
using Xunit;

namespace FleetIQ.Orders.UnitTests;

public class OrderTests
{
    private static Address Addr() => new("1 Main St", "Lisbon", "1000-001", "PT");
    private static Order NewOrder() =>
        Order.Create(Guid.NewGuid(), Addr(), Addr(), new[] { ("Pallet", 2, 120m) }).Value;

    [Fact]
    public void Create_WithItems_RaisesEvent()
    {
        var order = NewOrder();
        order.Status.ShouldBe(OrderStatus.Draft);
        order.Items.Count.ShouldBe(1);
        order.DomainEvents.OfType<OrderCreatedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Create_WithNoItems_Fails()
    {
        var result = Order.Create(Guid.NewGuid(), Addr(), Addr(), Array.Empty<(string, int, decimal)>());
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.NoItems);
    }

    [Fact]
    public void AddItem_OnDraft_Succeeds()
    {
        var order = NewOrder();
        order.AddItem("Box", 3, 10m).IsSuccess.ShouldBeTrue();
        order.Items.Count.ShouldBe(2);
    }

    [Fact]
    public void Confirm_ThenReadyForPlanning_FollowsLifecycle()
    {
        var order = NewOrder();
        order.Confirm().IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.MarkReadyForPlanning().IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.ReadyForPlanning);
    }

    [Fact]
    public void AddItem_AfterConfirm_Fails()
    {
        var order = NewOrder();
        order.Confirm();
        var result = order.AddItem("late", 1, 5m);
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.NotDraft);
    }

    [Fact]
    public void Cancel_WhenReadyForPlanning_Fails()
    {
        var order = NewOrder();
        order.Confirm();
        order.MarkReadyForPlanning();
        var result = order.Cancel();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.CannotCancelInPlanning);
    }
}
