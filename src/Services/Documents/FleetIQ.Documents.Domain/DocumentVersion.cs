using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Documents.Domain;

/// <summary>Immutable file version inside a Document. The aggregate's StorageRef always points at the latest one.</summary>
public sealed class DocumentVersion : Entity<Guid>
{
    public int Number { get; private set; }
    public string StorageRef { get; private set; } = string.Empty;
    public DateTime UploadedOnUtc { get; private set; }

    private DocumentVersion() { } // EF

    internal DocumentVersion(Guid id, int number, string storageRef) : base(id)
    {
        Number = number;
        StorageRef = storageRef;
        UploadedOnUtc = DateTime.UtcNow;
    }
}
