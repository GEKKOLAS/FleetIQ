using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Carriers.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Carriers.Infrastructure.Persistence;

public sealed class CarriersDbContext(DbContextOptions<CarriersDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("carriers");

        var carrier = modelBuilder.Entity<Carrier>();
        carrier.ToTable("carriers");
        carrier.HasKey(x => x.Id);
        carrier.Ignore(x => x.IsTransient);
        carrier.Ignore(x => x.TotalCapacityKg);
        carrier.Ignore(x => x.FleetSize);
        carrier.Ignore(x => x.DriverCount);
        carrier.Ignore(x => x.CanAcceptLoads);
        carrier.Property(x => x.Status).HasConversion<int>();
        carrier.HasIndex(x => x.Status);
        carrier.Property(x => x.LegalName).HasMaxLength(400);
        carrier.Property(x => x.RegistrationNumber).HasMaxLength(400);
        carrier.Property(x => x.Country).HasMaxLength(400);
        carrier.HasMany(x => x.Vehicles).WithOne().HasForeignKey("CarrierId").OnDelete(DeleteBehavior.Cascade);
        carrier.Navigation(x => x.Vehicles).UsePropertyAccessMode(PropertyAccessMode.Field);
        carrier.HasMany(x => x.Drivers).WithOne().HasForeignKey("CarrierId").OnDelete(DeleteBehavior.Cascade);
        carrier.Navigation(x => x.Drivers).UsePropertyAccessMode(PropertyAccessMode.Field);

        var vehicle = modelBuilder.Entity<Vehicle>();
        vehicle.ToTable("vehicles");
        vehicle.HasKey(x => x.Id);
        vehicle.Ignore(x => x.IsTransient);
        vehicle.Property(x => x.Plate).HasMaxLength(20);
        vehicle.Property(x => x.Type).HasConversion<int>();
        vehicle.Property(x => x.Status).HasConversion<int>();
        vehicle.Property(x => x.CapacityKg).HasPrecision(18, 2);

        var driver = modelBuilder.Entity<Driver>();
        driver.ToTable("drivers");
        driver.HasKey(x => x.Id);
        driver.Ignore(x => x.IsTransient);
        driver.Property(x => x.FullName).HasMaxLength(200);
        driver.Property(x => x.LicenceNumber).HasMaxLength(60);
        driver.Property(x => x.Status).HasConversion<int>();

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
