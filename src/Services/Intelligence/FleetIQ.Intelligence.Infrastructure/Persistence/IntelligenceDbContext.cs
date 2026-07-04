using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Intelligence.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Intelligence.Infrastructure.Persistence;

public sealed class IntelligenceDbContext(DbContextOptions<IntelligenceDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<PredictionRequest> PredictionRequests => Set<PredictionRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("intelligence");

        var e = modelBuilder.Entity<PredictionRequest>();
        e.ToTable("predictionrequests");
        e.HasKey(x => x.Id);
        e.Ignore(x => x.IsTransient);
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.ModelId).HasMaxLength(400);
        e.Property(x => x.PredictionType).HasMaxLength(20);
        e.Property(x => x.ResultJson).HasMaxLength(4000);
        e.Property(x => x.Confidence).HasPrecision(5, 4);
        e.Property(x => x.Error).HasMaxLength(1000);
        e.HasIndex(x => x.Status);

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
