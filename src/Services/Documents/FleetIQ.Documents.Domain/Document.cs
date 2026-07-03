using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Documents.Domain;

// NOTE: Store files in MinIO (S3 API); add OCR (managed API or Tesseract) later.
public enum DocumentStatus { Uploaded = 0, Verified = 1, Rejected = 2, Archived = 3 }

public static class DocumentErrors
{
    public static readonly Error InvalidTransition =
        new("Document.InvalidTransition", "The item is not in a state that allows this operation.");
    public static readonly Error OwnerRequired =
        new("Document.OwnerRequired", "An owner id is required.");
    public static readonly Error CategoryRequired =
        new("Document.CategoryRequired", "A category is required.");
    public static readonly Error StorageRefRequired =
        new("Document.StorageRefRequired", "A storage reference is required.");
    public static readonly Error ExpiryInPast =
        new("Document.ExpiryInPast", "The expiry date must be in the future.");
    public static readonly Error ReasonRequired =
        new("Document.ReasonRequired", "A rejection reason is required.");
    public static readonly Error Expired =
        new("Document.Expired", "The document has expired and cannot be verified; upload a new version.");
    public static readonly Error IsArchived =
        new("Document.Archived", "The document is archived and read-only.");
}

/// <summary>Document aggregate root. Lifecycle: Uploaded -> Verified | Rejected; a new version
/// returns it to Uploaded for re-verification; Archive makes it read-only.</summary>
public sealed class Document : AggregateRoot<Guid>
{
    private readonly List<DocumentVersion> _versions = new();

    public Guid OwnerId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string StorageRef { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? VerifiedOnUtc { get; private set; }
    public DateTime? ExpiresOnUtc { get; private set; }
    public string? RejectedReason { get; private set; }
    public IReadOnlyList<DocumentVersion> Versions => _versions.AsReadOnly();

    public bool IsExpired => ExpiresOnUtc is not null && ExpiresOnUtc < DateTime.UtcNow;

    private Document() { } // EF

    private Document(Guid id, Guid ownerId, string category, string storageRef, DateTime? expiresOnUtc) : base(id)
    {
        OwnerId = ownerId;
        Category = category;
        StorageRef = storageRef;
        ExpiresOnUtc = expiresOnUtc;
        Status = DocumentStatus.Uploaded;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<Document> Create(Guid ownerId, string category, string storageRef, DateTime? expiresOnUtc = null)
    {
        if (ownerId == Guid.Empty) return Result.Failure<Document>(DocumentErrors.OwnerRequired);
        if (string.IsNullOrWhiteSpace(category)) return Result.Failure<Document>(DocumentErrors.CategoryRequired);
        if (string.IsNullOrWhiteSpace(storageRef)) return Result.Failure<Document>(DocumentErrors.StorageRefRequired);
        if (expiresOnUtc is not null && expiresOnUtc <= DateTime.UtcNow) return Result.Failure<Document>(DocumentErrors.ExpiryInPast);

        var entity = new Document(Guid.NewGuid(), ownerId, category.Trim(), storageRef.Trim(), expiresOnUtc);
        entity._versions.Add(new DocumentVersion(Guid.NewGuid(), 1, entity.StorageRef));
        entity.Raise(new Events.DocumentCreatedDomainEvent(entity.Id));
        return Result.Success(entity);
    }

    public Result Verify()
    {
        if (Status != DocumentStatus.Uploaded) return Result.Failure(DocumentErrors.InvalidTransition);
        if (IsExpired) return Result.Failure(DocumentErrors.Expired);

        Status = DocumentStatus.Verified;
        VerifiedOnUtc = DateTime.UtcNow;
        RejectedReason = null;
        Raise(new Events.DocumentStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (Status != DocumentStatus.Uploaded) return Result.Failure(DocumentErrors.InvalidTransition);
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(DocumentErrors.ReasonRequired);

        Status = DocumentStatus.Rejected;
        RejectedReason = reason.Trim();
        Raise(new Events.DocumentStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    /// <summary>Uploads a replacement file. The document returns to Uploaded for re-verification.</summary>
    public Result AddVersion(string storageRef, DateTime? expiresOnUtc = null)
    {
        if (Status == DocumentStatus.Archived) return Result.Failure(DocumentErrors.IsArchived);
        if (string.IsNullOrWhiteSpace(storageRef)) return Result.Failure(DocumentErrors.StorageRefRequired);
        if (expiresOnUtc is not null && expiresOnUtc <= DateTime.UtcNow) return Result.Failure(DocumentErrors.ExpiryInPast);

        var number = _versions.Count == 0 ? 1 : _versions.Max(v => v.Number) + 1;
        _versions.Add(new DocumentVersion(Guid.NewGuid(), number, storageRef.Trim()));
        StorageRef = storageRef.Trim();
        if (expiresOnUtc is not null) ExpiresOnUtc = expiresOnUtc;
        Status = DocumentStatus.Uploaded;
        VerifiedOnUtc = null;
        RejectedReason = null;
        Raise(new Events.DocumentVersionAddedDomainEvent(Id, number));
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status == DocumentStatus.Archived) return Result.Failure(DocumentErrors.IsArchived);

        Status = DocumentStatus.Archived;
        Raise(new Events.DocumentStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
