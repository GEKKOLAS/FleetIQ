using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Billing.Infrastructure.Persistence;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("billing");

        var invoice = modelBuilder.Entity<Invoice>();
        invoice.ToTable("invoices");
        invoice.HasKey(x => x.Id);
        invoice.Ignore(x => x.IsTransient);
        invoice.Property(x => x.Currency).HasMaxLength(3);
        invoice.Property(x => x.Status).HasConversion<int>();
        invoice.HasIndex(x => x.CustomerId);
        invoice.HasIndex(x => x.Status);

        invoice.OwnsOne(x => x.Total, money =>
        {
            money.Property(p => p.Amount).HasColumnName("total_amount").HasPrecision(18, 2);
            money.Property(p => p.Currency).HasColumnName("total_currency").HasMaxLength(3);
        });

        invoice.OwnsMany(x => x.Lines, line =>
        {
            line.ToTable("invoice_lines");
            line.WithOwner().HasForeignKey("InvoiceId");
            line.HasKey("Id");
            line.Ignore(p => p.IsTransient);
            line.Property(p => p.Description).HasMaxLength(250);
            line.Property(p => p.UnitPrice).HasPrecision(18, 2);
            line.Ignore(p => p.LineTotal); // computed, not stored
        });

        var outbox = modelBuilder.Entity<OutboxMessage>();
        outbox.ToTable("outbox_messages");
        outbox.HasKey(x => x.Id);
        outbox.Property(x => x.Type).IsRequired();
        outbox.Property(x => x.Content).IsRequired();
        outbox.HasIndex(x => x.ProcessedOnUtc);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Same guard as Tracking: a brand-new child (InvoiceLine) added to an already-tracked
        // Invoice can be mis-classified by EF as Modified -> UPDATE that affects 0 rows ->
        // DbUpdateConcurrencyException (the AddLine 500). Forcing transient entities to Added
        // makes them INSERT correctly. Loaded rows have IsTransient = false, so they're untouched.
        foreach (var entry in ChangeTracker.Entries<Entity<Guid>>())
            if (entry.Entity.IsTransient && entry.State is EntityState.Modified or EntityState.Unchanged)
                entry.State = EntityState.Added;

        var messages = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Select(entry => entry.Entity)
            .SelectMany(aggregate =>
            {
                var events = aggregate.DomainEvents;
                aggregate.ClearDomainEvents();
                return events;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
            })
            .ToList();

        if (messages.Count > 0) OutboxMessages.AddRange(messages);
        return await base.SaveChangesAsync(ct);
    }
}
