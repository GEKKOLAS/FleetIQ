using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Billing.Domain;

namespace FleetIQ.Billing.Application.Abstractions;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken ct);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Invoice>> ListAsync(
        Guid? customerId, InvoiceStatus? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, string? sort, CancellationToken ct);
}
