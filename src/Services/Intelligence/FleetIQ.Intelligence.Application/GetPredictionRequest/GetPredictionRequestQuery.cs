using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Intelligence.Application.Abstractions;

namespace FleetIQ.Intelligence.Application.GetPredictionRequest;

public sealed record GetPredictionRequestQuery(Guid Id) : IQuery<PredictionRequestResponse?>;

public sealed record PredictionRequestResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetPredictionRequestQueryHandler(IPredictionRequestRepository repository)
    : IQueryHandler<GetPredictionRequestQuery, PredictionRequestResponse?>
{
    public async Task<PredictionRequestResponse?> Handle(GetPredictionRequestQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new PredictionRequestResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
