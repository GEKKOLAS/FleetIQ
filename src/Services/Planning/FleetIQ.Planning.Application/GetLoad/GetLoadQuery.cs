using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Planning.Application.Abstractions;

namespace FleetIQ.Planning.Application.GetLoad;

public sealed record GetLoadQuery(Guid Id) : IQuery<LoadDetailResponse?>;

public sealed record LoadOrderResponse(Guid OrderId, string Description, decimal WeightKg);

public sealed record LoadDetailResponse(
    Guid Id, string Reference, string Status, string OriginCity, string DestinationCity,
    decimal TotalWeightKg, Guid? CarrierId, Guid? RouteId,
    DateTime CreatedOnUtc, DateTime? PlannedOnUtc, DateTime? ConfirmedOnUtc, DateTime? CancelledOnUtc,
    IReadOnlyList<LoadOrderResponse> Orders);

public sealed class GetLoadQueryHandler(ILoadRepository repository)
    : IQueryHandler<GetLoadQuery, LoadDetailResponse?>
{
    public async Task<LoadDetailResponse?> Handle(GetLoadQuery query, CancellationToken ct)
    {
        var load = await repository.GetByIdAsync(query.Id, ct);
        if (load is null) return null;

        return new LoadDetailResponse(
            load.Id, load.Reference, load.Status.ToString(), load.OriginCity, load.DestinationCity,
            load.TotalWeightKg, load.CarrierId, load.RouteId,
            load.CreatedOnUtc, load.PlannedOnUtc, load.ConfirmedOnUtc, load.CancelledOnUtc,
            load.Orders.Select(o => new LoadOrderResponse(o.OrderId, o.Description, o.WeightKg)).ToList());
    }
}
