namespace FleetIQ.BuildingBlocks.Domain;

/// <summary>Immutable, identity-less concept compared by value.</summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj) =>
        obj is ValueObject other && GetType() == other.GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(0, (hash, comp) => HashCode.Combine(hash, comp));
}
