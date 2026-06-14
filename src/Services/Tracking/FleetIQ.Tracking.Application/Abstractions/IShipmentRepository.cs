using FleetIQ.Tracking.Domain;

namespace FleetIQ.Tracking.Application.Abstractions;

public interface IShipmentRepository
{
    Task AddAsync(Shipment entity, CancellationToken ct);
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct);
}
