using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Orders.Domain.Orders.Events;

namespace FleetIQ.Orders.Domain.Orders;

/// <summary>Order aggregate root. The only entry point for changing order state.</summary>
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Address Pickup { get; private set; } = null!;
    public Address Delivery { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public Money? Quote { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF

    private Order(Guid id, string orderNumber, Guid customerId, Address pickup, Address delivery) : base(id)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        Pickup = pickup;
        Delivery = delivery;
        Status = OrderStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<Order> Create(Guid customerId, Address pickup, Address delivery, IEnumerable<OrderLine> lines)
    {
        var lineList = lines.ToList();
        if (lineList.Count == 0) return Result.Failure<Order>(OrderErrors.NoItems);

        var order = new Order(Guid.NewGuid(), GenerateNumber(), customerId, pickup, delivery);
        foreach (var line in lineList)
            order._items.Add(new OrderItem(Guid.NewGuid(), line.Description, line.Quantity, line.WeightKg));

        order.Raise(new OrderCreatedDomainEvent(order.Id, order.OrderNumber, customerId));
        return Result.Success(order);
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Draft) return Result.Failure(OrderErrors.NotDraft);
        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmedDomainEvent(Id));
        return Result.Success();
    }

    private static string GenerateNumber() => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
}
