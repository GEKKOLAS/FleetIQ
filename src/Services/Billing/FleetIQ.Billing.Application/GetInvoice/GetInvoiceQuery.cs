using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Billing.Application.Abstractions;

namespace FleetIQ.Billing.Application.GetInvoice;

public sealed record GetInvoiceQuery(Guid Id) : IQuery<InvoiceDetailResponse?>;

public sealed record InvoiceLineResponse(string Description, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record InvoiceDetailResponse(
    Guid Id, Guid CustomerId, string Currency, string Status, decimal Total,
    DateTime? IssuedOnUtc, DateTime? DueDateUtc, DateTime? PaidOnUtc, DateTime CreatedOnUtc,
    IReadOnlyList<InvoiceLineResponse> Lines);

public sealed class GetInvoiceQueryHandler(IInvoiceRepository repository)
    : IQueryHandler<GetInvoiceQuery, InvoiceDetailResponse?>
{
    public async Task<InvoiceDetailResponse?> Handle(GetInvoiceQuery query, CancellationToken ct)
    {
        var invoice = await repository.GetByIdAsync(query.Id, ct);
        if (invoice is null) return null;

        return new InvoiceDetailResponse(
            invoice.Id, invoice.CustomerId, invoice.Currency, invoice.Status.ToString(), invoice.Total.Amount,
            invoice.IssuedOnUtc, invoice.DueDateUtc, invoice.PaidOnUtc, invoice.CreatedOnUtc,
            invoice.Lines.Select(l => new InvoiceLineResponse(l.Description, l.Quantity, l.UnitPrice, l.LineTotal)).ToList());
    }
}
