using FleetIQ.Integration.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Integration.UnitTests;

public class IntegrationConnectorTests
{
    private static IntegrationConnector NewAggregate() =>
        IntegrationConnector.Create("SAP ERP", "https://erp.example.com/api").Value;

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(IntegrationConnectorStatus.Registered);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_WithInvalidEndpoint_Fails()
    {
        IntegrationConnector.Create("X", "not-a-url").IsFailure.ShouldBeTrue();
        IntegrationConnector.Create("X", "ftp://files.example.com").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Activate_FromRegistered_AndFromDisabled_Succeeds()
    {
        var entity = NewAggregate();
        entity.Activate().IsSuccess.ShouldBeTrue();
        entity.Activate().IsFailure.ShouldBeTrue(); // already active

        entity.Disable("maintenance");
        entity.Activate().IsSuccess.ShouldBeTrue();
        entity.ConsecutiveFailures.ShouldBe(0);
        entity.DisabledReason.ShouldBeNull();
    }

    [Fact]
    public void RecordSync_RequiresActiveConnector()
    {
        var entity = NewAggregate();
        entity.RecordSyncSuccess().IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void SyncSuccess_ResetsFailureStreak()
    {
        var entity = NewAggregate();
        entity.Activate();
        entity.RecordSyncFailure("timeout");
        entity.RecordSyncFailure("timeout");
        entity.RecordSyncSuccess().IsSuccess.ShouldBeTrue();
        entity.ConsecutiveFailures.ShouldBe(0);
        entity.Status.ShouldBe(IntegrationConnectorStatus.Active);
    }

    [Fact]
    public void ThreeConsecutiveFailures_AutoDisable()
    {
        var entity = NewAggregate();
        entity.Activate();
        entity.RecordSyncFailure("timeout");
        entity.RecordSyncFailure("timeout");
        entity.Status.ShouldBe(IntegrationConnectorStatus.Active);

        entity.RecordSyncFailure("timeout");
        entity.Status.ShouldBe(IntegrationConnectorStatus.Disabled);
        entity.DisabledReason.ShouldNotBeNull();
        entity.DisabledReason!.ShouldContain("Auto-disabled");
    }

    [Fact]
    public void Disable_RequiresReason()
    {
        var entity = NewAggregate();
        entity.Activate();
        entity.Disable(" ").IsFailure.ShouldBeTrue();
        entity.Disable("maintenance window").IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(IntegrationConnectorStatus.Disabled);
    }
}
