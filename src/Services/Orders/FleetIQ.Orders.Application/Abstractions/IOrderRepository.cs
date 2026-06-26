using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Order>> ListAsync(Guid? customerId, OrderStatus? status, int page, int pageSize, string? sort, CancellationToken ct);
}
