using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Carriers.Application.Abstractions;

namespace FleetIQ.Carriers.Application.GetCarrier;

public sealed record GetCarrierQuery(Guid Id) : IQuery<CarrierResponse?>;

public sealed record CarrierResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetCarrierQueryHandler(ICarrierRepository repository)
    : IQueryHandler<GetCarrierQuery, CarrierResponse?>
{
    public async Task<CarrierResponse?> Handle(GetCarrierQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new CarrierResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
