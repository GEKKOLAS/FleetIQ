using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Tracking.Infrastructure.Persistence;

public sealed class ShipmentRepository(TrackingDbContext db) : IShipmentRepository
{
    public async Task AddAsync(Shipment shipment, CancellationToken ct) => await db.Shipments.AddAsync(shipment, ct);

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Shipments.Include(s => s.Events).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<PagedResult<Shipment>> ListAsync(Guid? carrierId, ShipmentStatus? status, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var q = db.Shipments.AsNoTracking().AsQueryable();
        if (carrierId is not null) q = q.Where(s => s.CarrierId == carrierId);
        if (status is not null) q = q.Where(s => s.Status == status);
        q = q.OrderByDescending(s => s.CreatedOnUtc);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Shipment>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Shipment>> GetActiveAsync(CancellationToken ct) =>
        await db.Shipments.AsNoTracking().Where(s => s.Status == ShipmentStatus.Dispatched || s.Status == ShipmentStatus.InTransit).ToListAsync(ct);
}
