using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Billing.Infrastructure.Persistence;

public sealed class InvoiceRepository(BillingDbContext db) : IInvoiceRepository
{
    public async Task AddAsync(Invoice invoice, CancellationToken ct) => await db.Invoices.AddAsync(invoice, ct);

    // Owned collections (Lines) are loaded automatically with the aggregate - no Include needed.
    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<PagedResult<Invoice>> ListAsync(
        Guid? customerId, InvoiceStatus? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, string? sort, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Invoices.AsNoTracking().AsQueryable();

        if (customerId is not null) query = query.Where(i => i.CustomerId == customerId);
        if (status is not null) query = query.Where(i => i.Status == status);
        if (fromUtc is not null) query = query.Where(i => i.CreatedOnUtc >= fromUtc);
        if (toUtc is not null) query = query.Where(i => i.CreatedOnUtc <= toUtc);

        query = sort?.ToLowerInvariant() switch
        {
            "created_asc" => query.OrderBy(i => i.CreatedOnUtc),
            "amount_desc" => query.OrderByDescending(i => i.Total.Amount),
            "amount_asc" => query.OrderBy(i => i.Total.Amount),
            _ => query.OrderByDescending(i => i.CreatedOnUtc)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Invoice>(items, page, pageSize, total);
    }
}
