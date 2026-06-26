using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error NotFound = new("Order.NotFound", "The order was not found.");
    public static readonly Error CustomerRequired = new("Order.CustomerRequired", "A customer is required.");
    public static readonly Error NoItems = new("Order.NoItems", "An order must have at least one item.");
    public static readonly Error NotDraft = new("Order.NotDraft", "Only a draft order can be modified.");
    public static readonly Error NotConfirmed = new("Order.NotConfirmed", "The order must be confirmed first.");
    public static readonly Error ItemDescriptionRequired = new("Order.ItemDescriptionRequired", "An item description is required.");
    public static readonly Error InvalidQuantity = new("Order.InvalidQuantity", "Quantity must be greater than zero.");
    public static readonly Error InvalidWeight = new("Order.InvalidWeight", "Weight cannot be negative.");
    public static readonly Error AlreadyCancelled = new("Order.AlreadyCancelled", "The order is already cancelled.");
    public static readonly Error CannotCancelInPlanning = new("Order.CannotCancelInPlanning", "An order ready for planning cannot be cancelled.");
}
