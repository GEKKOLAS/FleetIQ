using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Orders.Domain.Orders.Events;

namespace FleetIQ.Orders.Domain.Orders;

/// <summary>Order aggregate root. State changes go through intent-based methods (Confirm, Cancel,
/// MarkReadyForPlanning, AddItem) that enforce the order lifecycle.</summary>
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Address Pickup { get; private set; } = null!;
    public Address Delivery { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ConfirmedOnUtc { get; private set; }
    public DateTime? CancelledOnUtc { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    private Order(Guid id, string orderNumber, Guid customerId, Address pickup, Address delivery) : base(id)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        Pickup = pickup;
        Delivery = delivery;
        Status = OrderStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<Order> Create(Guid customerId, Address pickup, Address delivery,
        IEnumerable<(string Description, int Quantity, decimal WeightKg)> items)
    {
        if (customerId == Guid.Empty) return Result.Failure<Order>(OrderErrors.CustomerRequired);

        var list = items.ToList();
        if (list.Count == 0) return Result.Failure<Order>(OrderErrors.NoItems);

        var order = new Order(Guid.NewGuid(), GenerateNumber(), customerId, pickup, delivery);
        foreach (var i in list)
            order._items.Add(new OrderItem(Guid.NewGuid(), i.Description, i.Quantity, i.WeightKg));

        order.Raise(new OrderCreatedDomainEvent(order.Id, order.OrderNumber, customerId));
        return Result.Success(order);
    }

    public Result AddItem(string description, int quantity, decimal weightKg)
    {
        if (Status != OrderStatus.Draft) return Result.Failure(OrderErrors.NotDraft);
        if (string.IsNullOrWhiteSpace(description)) return Result.Failure(OrderErrors.ItemDescriptionRequired);
        if (quantity <= 0) return Result.Failure(OrderErrors.InvalidQuantity);
        if (weightKg < 0) return Result.Failure(OrderErrors.InvalidWeight);

        _items.Add(new OrderItem(Guid.NewGuid(), description, quantity, weightKg));
        Raise(new OrderItemAddedDomainEvent(Id, description, quantity, weightKg));
        return Result.Success();
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Draft) return Result.Failure(OrderErrors.NotDraft);
        if (_items.Count == 0) return Result.Failure(OrderErrors.NoItems);

        Status = OrderStatus.Confirmed;
        ConfirmedOnUtc = DateTime.UtcNow;
        Raise(new OrderConfirmedDomainEvent(Id));
        return Result.Success();
    }

    public Result MarkReadyForPlanning()
    {
        if (Status != OrderStatus.Confirmed) return Result.Failure(OrderErrors.NotConfirmed);

        Status = OrderStatus.ReadyForPlanning;
        Raise(new OrderReadyForPlanningDomainEvent(Id));
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == OrderStatus.Cancelled) return Result.Failure(OrderErrors.AlreadyCancelled);
        if (Status == OrderStatus.ReadyForPlanning) return Result.Failure(OrderErrors.CannotCancelInPlanning);

        Status = OrderStatus.Cancelled;
        CancelledOnUtc = DateTime.UtcNow;
        Raise(new OrderCancelledDomainEvent(Id));
        return Result.Success();
    }

    private static string GenerateNumber() => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
}
