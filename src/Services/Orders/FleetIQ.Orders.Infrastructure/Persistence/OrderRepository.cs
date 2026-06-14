using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Orders.Infrastructure.Persistence;

public sealed class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct) => await db.Orders.AddAsync(order, ct);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
}
