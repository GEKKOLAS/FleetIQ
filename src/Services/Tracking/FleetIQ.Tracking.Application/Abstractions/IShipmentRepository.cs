using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Tracking.Domain;

namespace FleetIQ.Tracking.Application.Abstractions;

public interface IShipmentRepository
{
    Task AddAsync(Shipment shipment, CancellationToken ct);
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Shipment>> ListAsync(Guid? carrierId, ShipmentStatus? status, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Shipment>> GetActiveAsync(CancellationToken ct);
}
