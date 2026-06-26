using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Tracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Tracking.Infrastructure.Persistence;

public sealed class TrackingDbContext(DbContextOptions<TrackingDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tracking");

        var shipment = modelBuilder.Entity<Shipment>();
        shipment.ToTable("shipments");
        shipment.HasKey(s => s.Id);
        shipment.Property(s => s.Status).HasConversion<int>();
        shipment.HasIndex(s => s.CarrierId);
        shipment.HasIndex(s => s.Status);
        shipment.Property(s => s.ReceivedBy).HasMaxLength(200);
        shipment.HasMany(s => s.Events).WithOne().HasForeignKey("ShipmentId").OnDelete(DeleteBehavior.Cascade);
        shipment.Navigation(s => s.Events).UsePropertyAccessMode(PropertyAccessMode.Field);

        var ev = modelBuilder.Entity<TrackingEvent>();
        ev.ToTable("tracking_events");
        ev.HasKey(e => e.Id);
        ev.Property(e => e.Type).HasConversion<int>();
        ev.Property(e => e.Note).HasMaxLength(500);

        var outbox = modelBuilder.Entity<OutboxMessage>();
        outbox.ToTable("outbox_messages");
        outbox.HasKey(m => m.Id);
        outbox.Property(m => m.Type).IsRequired();
        outbox.Property(m => m.Content).IsRequired();
        outbox.HasIndex(m => m.ProcessedOnUtc);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var messages = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Select(e => e.Entity)
            .SelectMany(a => { var ev = a.DomainEvents; a.ClearDomainEvents(); return ev; })
            .Select(de => new OutboxMessage { Type = de.GetType().AssemblyQualifiedName!, Content = JsonSerializer.Serialize(de, de.GetType()) })
            .ToList();
        if (messages.Count > 0) OutboxMessages.AddRange(messages);
        return await base.SaveChangesAsync(ct);
    }
}
