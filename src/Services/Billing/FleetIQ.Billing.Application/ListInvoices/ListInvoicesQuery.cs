using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;

namespace FleetIQ.Billing.Application.ListInvoices;

/// <summary>Paged, filterable list. sort: created_desc (default) | created_asc | amount_desc | amount_asc</summary>
public sealed record ListInvoicesQuery(
    Guid? CustomerId, string? Status, DateTime? FromUtc, DateTime? ToUtc,
    int Page, int PageSize, string? Sort) : IQuery<PagedResult<InvoiceSummaryResponse>>;

public sealed record InvoiceSummaryResponse(
    Guid Id, Guid CustomerId, string Status, decimal Total, string Currency, DateTime CreatedOnUtc);

public sealed class ListInvoicesQueryHandler(IInvoiceRepository repository)
    : IQueryHandler<ListInvoicesQuery, PagedResult<InvoiceSummaryResponse>>
{
    public async Task<PagedResult<InvoiceSummaryResponse>> Handle(ListInvoicesQuery query, CancellationToken ct)
    {
        InvoiceStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<InvoiceStatus>(query.Status, true, out var parsed))
            status = parsed;

        var paged = await repository.ListAsync(
            query.CustomerId, status, query.FromUtc, query.ToUtc, query.Page, query.PageSize, query.Sort, ct);

        var items = paged.Items
            .Select(i => new InvoiceSummaryResponse(i.Id, i.CustomerId, i.Status.ToString(), i.Total.Amount, i.Currency, i.CreatedOnUtc))
            .ToList();

        return new PagedResult<InvoiceSummaryResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
