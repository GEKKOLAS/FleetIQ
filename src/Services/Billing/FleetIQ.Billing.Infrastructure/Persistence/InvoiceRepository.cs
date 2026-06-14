using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Billing.Infrastructure.Persistence;

public sealed class InvoiceRepository(BillingDbContext db) : IInvoiceRepository
{
    public async Task AddAsync(Invoice entity, CancellationToken ct) => await db.Invoices.AddAsync(entity, ct);

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Invoices.FirstOrDefaultAsync(x => x.Id == id, ct);
}
