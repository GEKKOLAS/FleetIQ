using FleetIQ.Billing.Domain;

namespace FleetIQ.Billing.Application.Abstractions;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice entity, CancellationToken ct);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct);
}
