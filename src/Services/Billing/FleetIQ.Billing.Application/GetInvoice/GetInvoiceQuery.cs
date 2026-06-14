using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Billing.Application.Abstractions;

namespace FleetIQ.Billing.Application.GetInvoice;

public sealed record GetInvoiceQuery(Guid Id) : IQuery<InvoiceResponse?>;

public sealed record InvoiceResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetInvoiceQueryHandler(IInvoiceRepository repository)
    : IQueryHandler<GetInvoiceQuery, InvoiceResponse?>
{
    public async Task<InvoiceResponse?> Handle(GetInvoiceQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new InvoiceResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
