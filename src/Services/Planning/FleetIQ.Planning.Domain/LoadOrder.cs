using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Planning.Domain;

/// <summary>An order consolidated into a Load. Regular child entity (own table + FK to Load).</summary>
public sealed class LoadOrder : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal WeightKg { get; private set; }

    internal LoadOrder(Guid id, Guid orderId, string description, decimal weightKg) : base(id)
    {
        OrderId = orderId;
        Description = description;
        WeightKg = weightKg;
    }

    private LoadOrder() { } // EF: materialized row, IsTransient = false
}
