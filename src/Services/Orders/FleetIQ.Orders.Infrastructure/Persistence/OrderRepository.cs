using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Orders.Infrastructure.Persistence;

public sealed class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct) => await db.Orders.AddAsync(order, ct);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<PagedResult<Order>> ListAsync(
        Guid? customerId, OrderStatus? status, int page, int pageSize, string? sort, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Orders.AsNoTracking().AsQueryable();
        if (customerId is not null) query = query.Where(o => o.CustomerId == customerId);
        if (status is not null) query = query.Where(o => o.Status == status);

        query = sort?.ToLowerInvariant() switch
        {
            "created_asc" => query.OrderBy(o => o.CreatedOnUtc),
            _ => query.OrderByDescending(o => o.CreatedOnUtc)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Order>(items, page, pageSize, total);
    }
}
