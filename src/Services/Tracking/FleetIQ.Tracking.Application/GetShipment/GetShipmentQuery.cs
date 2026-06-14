using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Tracking.Application.Abstractions;

namespace FleetIQ.Tracking.Application.GetShipment;

public sealed record GetShipmentQuery(Guid Id) : IQuery<ShipmentResponse?>;

public sealed record ShipmentResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetShipmentQueryHandler(IShipmentRepository repository)
    : IQueryHandler<GetShipmentQuery, ShipmentResponse?>
{
    public async Task<ShipmentResponse?> Handle(GetShipmentQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new ShipmentResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
