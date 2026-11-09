using FleetIQ.Inventory.Domain;
using FleetIQ.Inventory.Domain.Events;
using Shouldly;
using Xunit;

namespace FleetIQ.Inventory.UnitTests;

public class InventoryItemTests
{
    private static InventoryItem NewItem(decimal reorderPoint = 10) =>
        InventoryItem.Create("pal-eur-1200", "EUR pallet", " lis-01 ", "unit", reorderPoint).Value;

    private static InventoryItem Stocked(decimal onHand, decimal reorderPoint = 10)
    {
        var item = NewItem(reorderPoint);
        item.Receive(onHand, "PO-1").IsSuccess.ShouldBeTrue();
        item.ClearDomainEvents();
        return item;
    }

    [Fact]
    public void Create_NormalizesCodes_StartsEmptyAndActive_AndRaisesCreatedEvent()
    {
        var item = NewItem();
        item.Sku.ShouldBe("PAL-EUR-1200");
        item.WarehouseCode.ShouldBe("LIS-01");
        item.Status.ShouldBe(InventoryItemStatus.Active);
        item.QuantityOnHand.ShouldBe(0);
        item.QuantityAvailable.ShouldBe(0);
        item.Movements.ShouldBeEmpty();
        item.DomainEvents.Single().ShouldBeOfType<InventoryItemCreatedDomainEvent>();
    }

    [Theory]
    [InlineData("", "Name", "WH", "unit", 0, "Inventory.SkuRequired")]
    [InlineData("SKU", " ", "WH", "unit", 0, "Inventory.NameRequired")]
    [InlineData("SKU", "Name", "", "unit", 0, "Inventory.WarehouseRequired")]
    [InlineData("SKU", "Name", "WH", "", 0, "Inventory.UnitRequired")]
    [InlineData("SKU", "Name", "WH", "unit", -1, "Inventory.NegativeReorderPoint")]
    public void Create_WithInvalidInput_Fails(string sku, string name, string wh, string uom, decimal reorder, string code)
    {
        var result = InventoryItem.Create(sku, name, wh, uom, reorder);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(code);
    }

    [Fact]
    public void Receive_IncreasesOnHand_AndRecordsMovement()
    {
        var item = NewItem();
        item.Receive(100, "PO-1").IsSuccess.ShouldBeTrue();

        item.QuantityOnHand.ShouldBe(100);
        item.QuantityAvailable.ShouldBe(100);
        var movement = item.Movements.Single();
        movement.Type.ShouldBe(StockMovementType.Receipt);
        movement.Quantity.ShouldBe(100);
        movement.OnHandAfter.ShouldBe(100);
        item.LastMovementOnUtc.ShouldBe(movement.OccurredOnUtc);
    }

    [Theory]
    [InlineData(0, "PO-1", "Inventory.QuantityMustBePositive")]
    [InlineData(-5, "PO-1", "Inventory.QuantityMustBePositive")]
    [InlineData(5, " ", "Inventory.ReferenceRequired")]
    public void Receive_WithInvalidInput_Fails(decimal qty, string reference, string code)
    {
        NewItem().Receive(qty, reference).Error.Code.ShouldBe(code);
    }

    [Fact]
    public void Reserve_ReducesAvailable_ButNotOnHand()
    {
        var item = Stocked(100);
        item.Reserve(30, "ORD-1").IsSuccess.ShouldBeTrue();

        item.QuantityOnHand.ShouldBe(100);
        item.QuantityReserved.ShouldBe(30);
        item.QuantityAvailable.ShouldBe(70);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_Fails_AndChangesNothing()
    {
        var item = Stocked(100);
        item.Reserve(80, "ORD-1");

        item.Reserve(21, "ORD-2").Error.ShouldBe(InventoryErrors.InsufficientAvailable);
        item.QuantityReserved.ShouldBe(80);
        item.Movements.Count.ShouldBe(2);
    }

    [Fact]
    public void ReleaseReservation_ReturnsStockToAvailable()
    {
        var item = Stocked(100);
        item.Reserve(30, "ORD-1");

        item.ReleaseReservation(30, "ORD-1").IsSuccess.ShouldBeTrue();
        item.QuantityReserved.ShouldBe(0);
        item.QuantityAvailable.ShouldBe(100);
    }

    [Fact]
    public void ReleaseReservation_MoreThanReserved_Fails()
    {
        var item = Stocked(100);
        item.Reserve(10, "ORD-1");
        item.ReleaseReservation(11, "ORD-1").Error.ShouldBe(InventoryErrors.InsufficientReserved);
    }

    [Fact]
    public void Ship_ConsumesReservationAndOnHand()
    {
        var item = Stocked(100);
        item.Reserve(30, "ORD-1");

        item.Ship(30, "ORD-1").IsSuccess.ShouldBeTrue();
        item.QuantityOnHand.ShouldBe(70);
        item.QuantityReserved.ShouldBe(0);
        item.QuantityAvailable.ShouldBe(70);
        item.Movements.Last().Type.ShouldBe(StockMovementType.Shipment);
    }

    [Fact]
    public void Ship_UnreservedStock_Fails()
    {
        var item = Stocked(100);
        item.Ship(1, "ORD-1").Error.ShouldBe(InventoryErrors.InsufficientReserved);
        item.QuantityOnHand.ShouldBe(100);
    }

    [Fact]
    public void Adjust_SetsCountedQuantity_AndRecordsSignedDelta()
    {
        var item = Stocked(100);

        item.Adjust(92, "Cycle count").IsSuccess.ShouldBeTrue();
        item.QuantityOnHand.ShouldBe(92);
        var movement = item.Movements.Last();
        movement.Type.ShouldBe(StockMovementType.Adjustment);
        movement.Quantity.ShouldBe(-8);
        movement.Reference.ShouldBe("Cycle count");
    }

    [Fact]
    public void Adjust_BelowReserved_Fails()
    {
        var item = Stocked(100);
        item.Reserve(50, "ORD-1");
        item.Adjust(40, "Count").Error.ShouldBe(InventoryErrors.CountBelowReserved);
    }

    [Fact]
    public void Adjust_RequiresReason_PositiveCount_AndAChange()
    {
        var item = Stocked(100);
        item.Adjust(90, "").Error.ShouldBe(InventoryErrors.ReasonRequired);
        item.Adjust(-1, "Count").Error.ShouldBe(InventoryErrors.NegativeCount);
        item.Adjust(100, "Count").Error.ShouldBe(InventoryErrors.CountUnchanged);
    }

    [Fact]
    public void Reserve_CrossingReorderPoint_RaisesLowStockOnce()
    {
        var item = Stocked(50, reorderPoint: 20);

        item.Reserve(25, "ORD-1");
        item.DomainEvents.OfType<LowStockDetectedDomainEvent>().ShouldBeEmpty();

        item.Reserve(10, "ORD-2"); // available 15 <= 20
        item.IsBelowReorderPoint.ShouldBeTrue();
        var low = item.DomainEvents.OfType<LowStockDetectedDomainEvent>().Single();
        low.Available.ShouldBe(15);
        low.WarehouseCode.ShouldBe("LIS-01");

        item.Reserve(5, "ORD-3"); // still low: no second alert
        item.DomainEvents.OfType<LowStockDetectedDomainEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void Receive_AboveReorderPoint_ClearsLowStock_SoTheNextDropAlertsAgain()
    {
        var item = Stocked(15, reorderPoint: 20);
        item.IsBelowReorderPoint.ShouldBeTrue();

        item.Receive(100, "PO-2");
        item.IsBelowReorderPoint.ShouldBeFalse();

        item.Adjust(10, "Damaged in storage");
        item.DomainEvents.OfType<LowStockDetectedDomainEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void Discontinue_WithReservations_Fails()
    {
        var item = Stocked(10);
        item.Reserve(1, "ORD-1");
        item.Discontinue().Error.ShouldBe(InventoryErrors.HasReservations);
    }

    [Fact]
    public void Discontinued_Item_RejectsReceiptsAndReservations_ButAllowsWriteOff()
    {
        var item = Stocked(10);
        item.Discontinue().IsSuccess.ShouldBeTrue();

        item.Receive(1, "PO-2").Error.ShouldBe(InventoryErrors.IsDiscontinued);
        item.Reserve(1, "ORD-1").Error.ShouldBe(InventoryErrors.IsDiscontinued);
        item.Discontinue().Error.ShouldBe(InventoryErrors.IsDiscontinued);
        item.IsBelowReorderPoint.ShouldBeFalse();

        item.Adjust(0, "Written off").IsSuccess.ShouldBeTrue();
        item.QuantityOnHand.ShouldBe(0);
    }

    [Fact]
    public void ChangeReorderPoint_RejectsNegative_AndCanTriggerLowStock()
    {
        var item = Stocked(30, reorderPoint: 10);
        item.ChangeReorderPoint(-1).Error.ShouldBe(InventoryErrors.NegativeReorderPoint);

        item.ChangeReorderPoint(40).IsSuccess.ShouldBeTrue();
        item.DomainEvents.OfType<LowStockDetectedDomainEvent>().ShouldHaveSingleItem();
    }
}
