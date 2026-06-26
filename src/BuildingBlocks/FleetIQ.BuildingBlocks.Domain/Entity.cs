using System.ComponentModel.DataAnnotations.Schema;

namespace FleetIQ.BuildingBlocks.Domain;

/// <summary>Base class for entities, compared by identity.</summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    /// <summary>True when the instance was constructed in-memory as a brand-new entity
    /// (vs. materialized from the database by EF, which uses the parameterless ctor).
    /// Used by the DbContext to guarantee new children are INSERTed, never UPDATEd.
    /// Not mapped to a column, so it requires no migration and affects no other service.</summary>
    [NotMapped]
    public bool IsTransient { get; private set; }

    protected Entity(TId id)
    {
        Id = id;
        IsTransient = true;
    }

    // EF materialization ctor: IsTransient stays false (this row already exists).
    protected Entity() { }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    public override int GetHashCode() => Id.GetHashCode();
}
