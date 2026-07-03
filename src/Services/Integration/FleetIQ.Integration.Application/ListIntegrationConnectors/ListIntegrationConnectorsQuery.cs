using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Integration.Application.Abstractions;

namespace FleetIQ.Integration.Application.ListIntegrationConnectors;

public sealed record ListIntegrationConnectorsQuery(int Take) : IQuery<IReadOnlyList<IntegrationConnectorListItem>>;

public sealed record IntegrationConnectorListItem(
    Guid Id, string SystemName, string Endpoint, string Status, DateTime CreatedOnUtc,
    DateTime? LastSyncOnUtc, int ConsecutiveFailures, string? DisabledReason);

public sealed class ListIntegrationConnectorsQueryHandler(IIntegrationConnectorRepository repository)
    : IQueryHandler<ListIntegrationConnectorsQuery, IReadOnlyList<IntegrationConnectorListItem>>
{
    public async Task<IReadOnlyList<IntegrationConnectorListItem>> Handle(ListIntegrationConnectorsQuery query, CancellationToken ct)
    {
        var items = await repository.ListRecentAsync(query.Take, ct);
        return items
            .Select(c => new IntegrationConnectorListItem(
                c.Id, c.SystemName, c.Endpoint, c.Status.ToString(), c.CreatedOnUtc,
                c.LastSyncOnUtc, c.ConsecutiveFailures, c.DisabledReason))
            .ToList();
    }
}
