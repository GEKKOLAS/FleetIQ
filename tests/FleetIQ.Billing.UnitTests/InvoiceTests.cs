using FleetIQ.Billing.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Billing.UnitTests;

public class InvoiceTests
{
    private static Invoice NewAggregate() => Invoice.Create(Guid.NewGuid(), 10m, "Currency");

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(InvoiceStatus.Draft);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Issue_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Issue();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(InvoiceStatus.Issued);
    }

    [Fact]
    public void Issue_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Issue();
        var second = entity.Issue();
        second.IsFailure.ShouldBeTrue();
    }
}
