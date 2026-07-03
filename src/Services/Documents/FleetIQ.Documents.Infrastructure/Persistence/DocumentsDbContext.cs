using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Documents.Infrastructure.Persistence;

public sealed class DocumentsDbContext(DbContextOptions<DocumentsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("documents");

        var e = modelBuilder.Entity<Document>();
        e.ToTable("documents");
        e.HasKey(x => x.Id);
        e.Ignore(x => x.IsTransient);
        e.Ignore(x => x.IsExpired);
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.Category).HasMaxLength(400);
        e.Property(x => x.StorageRef).HasMaxLength(400);
        e.Property(x => x.RejectedReason).HasMaxLength(500);
        e.HasIndex(x => x.OwnerId);
        e.HasIndex(x => x.Status);

        e.OwnsMany(x => x.Versions, v =>
        {
            v.ToTable("document_versions");
            v.WithOwner().HasForeignKey("DocumentId");
            v.HasKey("Id");
            v.Ignore(p => p.IsTransient);
            v.Property(p => p.StorageRef).HasMaxLength(400);
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
        // Same guard as Billing/Tracking: a brand-new child (DocumentVersion) added to an
        // already-tracked Document can be mis-classified by EF as Modified -> UPDATE affecting
        // 0 rows -> DbUpdateConcurrencyException. Forcing transient entities to Added fixes it.
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
