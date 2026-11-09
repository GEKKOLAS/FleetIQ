using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Inventory.Application.Abstractions;
using FleetIQ.Inventory.Domain;
using FluentValidation;

namespace FleetIQ.Inventory.Application.CreateInventoryItem;

public sealed record CreateInventoryItemCommand(
    string Sku, string Name, string WarehouseCode, string UnitOfMeasure, decimal ReorderPoint = 0) : ICommand<Result<Guid>>;

public sealed class CreateInventoryItemCommandValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WarehouseCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(16);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateInventoryItemCommandHandler(IInventoryItemRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateInventoryItemCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInventoryItemCommand command, CancellationToken ct)
    {
        var created = InventoryItem.Create(command.Sku, command.Name, command.WarehouseCode, command.UnitOfMeasure, command.ReorderPoint);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error);

        // One stock record per SKU per warehouse (also enforced by a unique index).
        if (await repository.ExistsAsync(created.Value.Sku, created.Value.WarehouseCode, ct))
            return Result.Failure<Guid>(InventoryErrors.DuplicateSku);

        await repository.AddAsync(created.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(created.Value.Id);
    }
}
