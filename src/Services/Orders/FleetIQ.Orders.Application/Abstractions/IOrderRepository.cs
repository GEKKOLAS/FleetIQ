using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
}
