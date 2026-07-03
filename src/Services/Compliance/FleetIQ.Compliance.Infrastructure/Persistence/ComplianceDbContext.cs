using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Compliance.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Compliance.Infrastructure.Persistence;

public sealed class ComplianceDbContext(DbContextOptions<ComplianceDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<ComplianceCheck> ComplianceChecks => Set<ComplianceCheck>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("compliance");

        var e = modelBuilder.Entity<ComplianceCheck>();
        e.ToTable("compliancechecks");
        e.HasKey(x => x.Id);
        e.Ignore(x => x.IsTransient);
        e.Ignore(x => x.HasBlockingFindings);
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.SubjectType).HasMaxLength(20);
        e.Property(x => x.RuleCode).HasMaxLength(400);
        e.Property(x => x.Jurisdiction).HasMaxLength(400);
        e.Property(x => x.WaivedReason).HasMaxLength(500);
        e.HasIndex(x => x.SubjectId);
        e.HasIndex(x => x.Status);

        e.OwnsMany(x => x.Findings, f =>
        {
            f.ToTable("compliance_findings");
            f.WithOwner().HasForeignKey("ComplianceCheckId");
            f.HasKey("Id");
            f.Ignore(p => p.IsTransient);
            f.Property(p => p.Code).HasMaxLength(100);
            f.Property(p => p.Severity).HasMaxLength(10);
            f.Property(p => p.Message).HasMaxLength(1000);
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
        // Same guard as Billing/Tracking: a brand-new child (ComplianceFinding) added to an
        // already-tracked ComplianceCheck can be mis-classified by EF as Modified -> UPDATE
        // affecting 0 rows. Forcing transient entities to Added fixes it.
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
