using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Planning.Domain;
using FleetIQ.Planning.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Planning.Infrastructure.Persistence;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Load> Loads => Set<Load>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<CarrierView> CarrierViews => Set<CarrierView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("planning");

        var load = modelBuilder.Entity<Load>();
        load.ToTable("loads");
        load.HasKey(x => x.Id);
        load.Ignore(x => x.IsTransient);
        load.Property(x => x.Reference).HasMaxLength(40);
        load.HasIndex(x => x.Reference).IsUnique();
        load.Property(x => x.Status).HasConversion<int>();
        load.HasIndex(x => x.Status);
        load.Property(x => x.OriginCity).HasMaxLength(400);
        load.Property(x => x.DestinationCity).HasMaxLength(400);
        load.Property(x => x.TotalWeightKg).HasPrecision(18, 2);
        load.HasMany(x => x.Orders).WithOne().HasForeignKey("LoadId").OnDelete(DeleteBehavior.Cascade);
        load.Navigation(x => x.Orders).UsePropertyAccessMode(PropertyAccessMode.Field);

        var loadOrder = modelBuilder.Entity<LoadOrder>();
        loadOrder.ToTable("load_orders");
        loadOrder.HasKey(x => x.Id);
        loadOrder.Ignore(x => x.IsTransient);
        loadOrder.Property(x => x.Description).HasMaxLength(400);
        loadOrder.Property(x => x.WeightKg).HasPrecision(18, 2);

        var route = modelBuilder.Entity<Route>();
        route.ToTable("routes");
        route.HasKey(x => x.Id);
        route.Ignore(x => x.IsTransient);
        route.Property(x => x.Status).HasConversion<int>();
        route.HasIndex(x => x.LoadId);
        route.HasMany(x => x.Stops).WithOne().HasForeignKey("RouteId").OnDelete(DeleteBehavior.Cascade);
        route.Navigation(x => x.Stops).UsePropertyAccessMode(PropertyAccessMode.Field);

        var stop = modelBuilder.Entity<RouteStop>();
        stop.ToTable("route_stops");
        stop.HasKey(x => x.Id);
        stop.Ignore(x => x.IsTransient);
        stop.Property(x => x.Kind).HasConversion<int>();
        stop.Property(x => x.City).HasMaxLength(200);
        stop.Property(x => x.Address).HasMaxLength(400);

        var carrierView = modelBuilder.Entity<CarrierView>();
        carrierView.ToTable("carrier_views");
        carrierView.HasKey(x => x.CarrierId);
        carrierView.Property(x => x.LegalName).HasMaxLength(400);

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
        // Same guard as Tracking/Billing: a brand-new child entity added to an already-tracked
        // aggregate can be mis-classified by EF as Modified -> UPDATE that affects 0 rows ->
        // DbUpdateConcurrencyException. Forcing transient entities to Added makes them INSERT.
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
