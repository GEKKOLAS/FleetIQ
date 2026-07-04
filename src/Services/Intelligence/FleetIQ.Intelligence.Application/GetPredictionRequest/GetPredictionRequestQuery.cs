using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Intelligence.Application.Abstractions;

namespace FleetIQ.Intelligence.Application.GetPredictionRequest;

public sealed record GetPredictionRequestQuery(Guid Id) : IQuery<PredictionRequestResponse?>;

public sealed record PredictionRequestResponse(
    Guid Id, string ModelId, string PredictionType, string Status, DateTime CreatedOnUtc,
    DateTime? StartedOnUtc, DateTime? FinishedOnUtc, string? ResultJson, decimal? Confidence, string? Error);

public sealed class GetPredictionRequestQueryHandler(IPredictionRequestRepository repository)
    : IQueryHandler<GetPredictionRequestQuery, PredictionRequestResponse?>
{
    public async Task<PredictionRequestResponse?> Handle(GetPredictionRequestQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null
            ? null
            : new PredictionRequestResponse(
                entity.Id, entity.ModelId, entity.PredictionType, entity.Status.ToString(), entity.CreatedOnUtc,
                entity.StartedOnUtc, entity.FinishedOnUtc, entity.ResultJson, entity.Confidence, entity.Error);
    }
}
