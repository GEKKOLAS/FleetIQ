using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Analytics.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Analytics.Infrastructure.Persistence;

public sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("analytics");

        var e = modelBuilder.Entity<ReportDefinition>();
        e.ToTable("reportdefinitions");
        e.HasKey(x => x.Id);
        e.Ignore(x => x.IsTransient);
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.Name).HasMaxLength(400);
        e.Property(x => x.Query).HasMaxLength(2000);
        e.Property(x => x.Schedule).HasMaxLength(20);
        e.HasIndex(x => x.Status);

        e.OwnsMany(x => x.Runs, r =>
        {
            r.ToTable("report_runs");
            r.WithOwner().HasForeignKey("ReportDefinitionId");
            r.HasKey("Id");
            r.Ignore(p => p.IsTransient);
            r.Property(p => p.Error).HasMaxLength(1000);
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
        // Same guard as Billing/Tracking: a brand-new child (ReportRun) added to an already-tracked
        // ReportDefinition can be mis-classified by EF as Modified -> UPDATE affecting 0 rows.
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
