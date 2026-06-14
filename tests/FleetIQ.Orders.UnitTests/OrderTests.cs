using FleetIQ.Orders.Domain.Orders;
using FleetIQ.Orders.Domain.Orders.Events;
using Shouldly;
using Xunit;

namespace FleetIQ.Orders.UnitTests;

public class OrderTests
{
    private static Address SampleAddress() => new("1 Main St", "Lisbon", "1000-001", "PT");

    [Fact]
    public void Create_WithItems_RaisesOrderCreatedEvent()
    {
        var order = Order.Create(Guid.NewGuid(), SampleAddress(), SampleAddress(),
            new[] { new OrderLine("Pallet", 2, 120m) });

        order.IsSuccess.ShouldBeTrue();
        order.Value.Status.ShouldBe(OrderStatus.Draft);
        order.Value.Items.Count.ShouldBe(1);
        order.Value.DomainEvents.OfType<OrderCreatedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Create_WithNoItems_Fails()
    {
        var order = Order.Create(Guid.NewGuid(), SampleAddress(), SampleAddress(), Enumerable.Empty<OrderLine>());
        order.IsFailure.ShouldBeTrue();
        order.Error.ShouldBe(OrderErrors.NoItems);
    }

    [Fact]
    public void Confirm_DraftOrder_TransitionsToConfirmed()
    {
        var order = Order.Create(Guid.NewGuid(), SampleAddress(), SampleAddress(),
            new[] { new OrderLine("Pallet", 1, 50m) }).Value;

        var result = order.Confirm();

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.DomainEvents.OfType<OrderConfirmedDomainEvent>().ShouldHaveSingleItem();
    }
}
