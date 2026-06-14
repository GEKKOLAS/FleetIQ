using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Planning.Application.Abstractions;

namespace FleetIQ.Planning.Application.GetLoad;

public sealed record GetLoadQuery(Guid Id) : IQuery<LoadResponse?>;

public sealed record LoadResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetLoadQueryHandler(ILoadRepository repository)
    : IQueryHandler<GetLoadQuery, LoadResponse?>
{
    public async Task<LoadResponse?> Handle(GetLoadQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new LoadResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
