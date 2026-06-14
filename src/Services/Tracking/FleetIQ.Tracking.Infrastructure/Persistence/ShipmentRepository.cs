using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Tracking.Infrastructure.Persistence;

public sealed class ShipmentRepository(TrackingDbContext db) : IShipmentRepository
{
    public async Task AddAsync(Shipment entity, CancellationToken ct) => await db.Shipments.AddAsync(entity, ct);

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Shipments.FirstOrDefaultAsync(x => x.Id == id, ct);
}
