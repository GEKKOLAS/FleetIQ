using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        var e = modelBuilder.Entity<InventoryItem>();
        e.ToTable("inventory_items");
        e.HasKey(x => x.Id);
        e.Ignore(x => x.IsTransient);
        e.Ignore(x => x.QuantityAvailable);
        e.Ignore(x => x.IsBelowReorderPoint);
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.Property(x => x.WarehouseCode).HasMaxLength(32).IsRequired();
        e.Property(x => x.UnitOfMeasure).HasMaxLength(16).IsRequired();
        e.Property(x => x.QuantityOnHand).HasPrecision(18, 3);
        e.Property(x => x.QuantityReserved).HasPrecision(18, 3);
        e.Property(x => x.ReorderPoint).HasPrecision(18, 3);
        e.HasIndex(x => new { x.WarehouseCode, x.Sku }).IsUnique();
        e.HasIndex(x => x.Status);
        // Optimistic concurrency on Postgres' xmin: two concurrent reservations can't both pass the
        // "enough available stock" check and oversell the item.
        e.Property<uint>("Version").IsRowVersion();

        e.OwnsMany(x => x.Movements, m =>
        {
            m.ToTable("stock_movements");
            m.WithOwner().HasForeignKey("InventoryItemId");
            m.HasKey("Id");
            m.Ignore(p => p.IsTransient);
            m.Property(p => p.Type).HasConversion<int>();
            m.Property(p => p.Reference).HasMaxLength(200);
            m.Property(p => p.Quantity).HasPrecision(18, 3);
            m.Property(p => p.OnHandAfter).HasPrecision(18, 3);
            m.Property(p => p.ReservedAfter).HasPrecision(18, 3);
            m.HasIndex("InventoryItemId", nameof(StockMovement.OccurredOnUtc));
        });

        var o = modelBuilder.Entity<OutboxMessage>();
        o.ToTable("outbox_messages");
        o.HasKey(x => x.Id);
        o.Property(x => x.Type).IsRequired();
        o.Property(x => x.Content).IsRequired();
        o.HasIndex(x => x.ProcessedOnUtc);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Same guard as Billing/Documents: a brand-new child (StockMovement) added to an already-tracked
        // InventoryItem can be mis-classified by EF as Modified -> UPDATE affecting 0 rows. Force Added.
        foreach (var entry in ChangeTracker.Entries<Entity<Guid>>())
            if (entry.Entity.IsTransient && entry.State is EntityState.Modified or EntityState.Unchanged)
                entry.State = EntityState.Added;

        var messages = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Select(entry => entry.Entity)
            .SelectMany(aggregate =>
            {
                // Copy first: DomainEvents is a read-only *view* over the aggregate's list, so
                // clearing before copying would leave nothing to write to the outbox.
                var events = aggregate.DomainEvents.ToList();
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
