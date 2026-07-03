using FleetIQ.Documents.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Documents.UnitTests;

public class DocumentTests
{
    private static Document NewAggregate() => Document.Create(Guid.NewGuid(), "CMR", "s3://docs/a.pdf").Value;

    [Fact]
    public void Create_SetsInitialStatus_SeedsFirstVersion_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(DocumentStatus.Uploaded);
        entity.Versions.Count.ShouldBe(1);
        entity.Versions[0].Number.ShouldBe(1);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_WithoutOwner_Fails()
    {
        var result = Document.Create(Guid.Empty, "CMR", "s3://docs/a.pdf");
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(DocumentErrors.OwnerRequired);
    }

    [Fact]
    public void Create_WithPastExpiry_Fails()
    {
        var result = Document.Create(Guid.NewGuid(), "Insurance", "s3://docs/a.pdf", DateTime.UtcNow.AddDays(-1));
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(DocumentErrors.ExpiryInPast);
    }

    [Fact]
    public void Verify_FromUploaded_Succeeds_AndStampsTimestamp()
    {
        var entity = NewAggregate();
        var result = entity.Verify();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(DocumentStatus.Verified);
        entity.VerifiedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Verify_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Verify();
        entity.Verify().IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Reject_RequiresReason()
    {
        var entity = NewAggregate();
        entity.Reject("  ").IsFailure.ShouldBeTrue();
        entity.Reject("Illegible scan").IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(DocumentStatus.Rejected);
        entity.RejectedReason.ShouldBe("Illegible scan");
    }

    [Fact]
    public void AddVersion_AfterRejection_ReturnsToUploaded_AndBumpsNumber()
    {
        var entity = NewAggregate();
        entity.Reject("Illegible scan");

        var result = entity.AddVersion("s3://docs/a-v2.pdf");

        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(DocumentStatus.Uploaded);
        entity.StorageRef.ShouldBe("s3://docs/a-v2.pdf");
        entity.RejectedReason.ShouldBeNull();
        entity.Versions.Count.ShouldBe(2);
        entity.Versions[1].Number.ShouldBe(2);
    }

    [Fact]
    public void AddVersion_ClearsVerification()
    {
        var entity = NewAggregate();
        entity.Verify();
        entity.AddVersion("s3://docs/a-v2.pdf");
        entity.Status.ShouldBe(DocumentStatus.Uploaded);
        entity.VerifiedOnUtc.ShouldBeNull();
    }

    [Fact]
    public void Archive_MakesDocumentReadOnly()
    {
        var entity = NewAggregate();
        entity.Archive().IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(DocumentStatus.Archived);
        entity.AddVersion("s3://docs/a-v2.pdf").IsFailure.ShouldBeTrue();
        entity.Verify().IsFailure.ShouldBeTrue();
        entity.Archive().IsFailure.ShouldBeTrue();
    }
}
