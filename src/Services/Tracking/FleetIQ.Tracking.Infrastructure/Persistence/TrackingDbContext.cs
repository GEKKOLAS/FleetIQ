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

        var e = modelBuilder.Entity<Shipment>();
        e.ToTable("shipments");
        e.HasKey(x => x.Id);
        e.Property(x => x.Status).HasConversion<int>();


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
