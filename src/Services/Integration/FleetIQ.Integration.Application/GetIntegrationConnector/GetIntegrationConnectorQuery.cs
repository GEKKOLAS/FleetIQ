using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Integration.Application.Abstractions;

namespace FleetIQ.Integration.Application.GetIntegrationConnector;

public sealed record GetIntegrationConnectorQuery(Guid Id) : IQuery<IntegrationConnectorResponse?>;

public sealed record IntegrationConnectorResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetIntegrationConnectorQueryHandler(IIntegrationConnectorRepository repository)
    : IQueryHandler<GetIntegrationConnectorQuery, IntegrationConnectorResponse?>
{
    public async Task<IntegrationConnectorResponse?> Handle(GetIntegrationConnectorQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new IntegrationConnectorResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
