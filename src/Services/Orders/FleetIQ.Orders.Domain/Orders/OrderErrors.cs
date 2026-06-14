using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error NoItems = new("Order.NoItems", "An order must contain at least one item.");
    public static readonly Error NotDraft = new("Order.NotDraft", "Only a draft order can be confirmed.");
    public static readonly Error AlreadyCancelled = new("Order.AlreadyCancelled", "Order is already cancelled.");
}
