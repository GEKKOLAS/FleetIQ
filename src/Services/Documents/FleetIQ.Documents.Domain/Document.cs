using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Documents.Domain;

// NOTE: Store files in MinIO (S3 API); add OCR (managed API or Tesseract) and DocumentVersion entity.
public enum DocumentStatus { Uploaded = 0, Verified = 1 }

public static class DocumentErrors
{
    public static readonly Error InvalidTransition =
        new("Document.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>Document aggregate root.</summary>
public sealed class Document : AggregateRoot<Guid>
{
    public Guid OwnerId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string StorageRef { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private Document() { } // EF

    private Document(Guid id, Guid ownerId, string category, string storageRef) : base(id)
    {
        OwnerId = ownerId;
        Category = category;
        StorageRef = storageRef;
        Status = DocumentStatus.Uploaded;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Document Create(Guid ownerId, string category, string storageRef)
    {
        var entity = new Document(Guid.NewGuid(), ownerId, category, storageRef);
        entity.Raise(new Events.DocumentCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Verify()
    {
        if (Status != DocumentStatus.Uploaded) return Result.Failure(DocumentErrors.InvalidTransition);
        Status = DocumentStatus.Verified;
        Raise(new Events.DocumentStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
