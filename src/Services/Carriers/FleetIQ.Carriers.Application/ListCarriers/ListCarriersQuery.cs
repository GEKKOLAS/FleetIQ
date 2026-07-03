using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.ListCarriers;

public sealed record ListCarriersQuery(string? Status, int Page, int PageSize) : IQuery<PagedResult<CarrierSummaryResponse>>;

public sealed record CarrierSummaryResponse(
    Guid Id, string LegalName, string Country, string Status,
    decimal TotalCapacityKg, int FleetSize, int DriverCount, DateTime CreatedOnUtc);

public sealed class ListCarriersQueryHandler(ICarrierRepository repository)
    : IQueryHandler<ListCarriersQuery, PagedResult<CarrierSummaryResponse>>
{
    public async Task<PagedResult<CarrierSummaryResponse>> Handle(ListCarriersQuery query, CancellationToken ct)
    {
        CarrierStatus? status = Enum.TryParse<CarrierStatus>(query.Status, true, out var s) ? s : null;
        var page = await repository.ListAsync(status, query.Page, query.PageSize, ct);
        var items = page.Items.Select(c => new CarrierSummaryResponse(
            c.Id, c.LegalName, c.Country, c.Status.ToString(),
            c.TotalCapacityKg, c.FleetSize, c.DriverCount, c.CreatedOnUtc)).ToList();
        return new PagedResult<CarrierSummaryResponse>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
