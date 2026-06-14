namespace FleetIQ.BuildingBlocks.Infrastructure.Outbox;

/// <summary>Transactional outbox row. Domain events are saved here in the SAME transaction
/// as the aggregate, then published to the bus by <see cref="OutboxProcessor"/>.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Type { get; init; }          // assembly-qualified event type name
    public required string Content { get; init; }       // JSON payload
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
