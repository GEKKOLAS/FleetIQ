using FleetIQ.Documents.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Documents.UnitTests;

public class DocumentTests
{
    private static Document NewAggregate() => Document.Create(Guid.NewGuid(), "Category", "StorageRef");

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(DocumentStatus.Uploaded);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Verify_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Verify();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(DocumentStatus.Verified);
    }

    [Fact]
    public void Verify_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Verify();
        var second = entity.Verify();
        second.IsFailure.ShouldBeTrue();
    }
}
