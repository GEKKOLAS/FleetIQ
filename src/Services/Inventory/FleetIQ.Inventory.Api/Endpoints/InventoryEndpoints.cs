using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Inventory.Application.Abstractions;
using FleetIQ.Inventory.Application.CreateInventoryItem;
using FleetIQ.Inventory.Application.GetInventoryItem;
using FleetIQ.Inventory.Application.ListInventoryItems;
using FleetIQ.Inventory.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Inventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventoryitems").WithTags("Inventory").RequireAuthorization();

        group.MapPost("/", async (
            CreateInventoryItemCommand command,
            IValidator<CreateInventoryItemCommand> validator,
            ICommandHandler<CreateInventoryItemCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            if (result.IsSuccess) return Results.Created($"/inventoryitems/{result.Value}", new { id = result.Value });
            return result.Error == InventoryErrors.DuplicateSku
                ? Results.Conflict(new { result.Error.Code, result.Error.Description })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        // LIST - powers the Inventory page in the SPA. ?warehouse=LIS-01&lowStock=true&take=50
        group.MapGet("/", async (
            string? warehouse,
            bool? lowStock,
            int? take,
            IQueryHandler<ListInventoryItemsQuery, IReadOnlyList<InventoryItemListItem>> handler,
            CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ListInventoryItemsQuery(warehouse, lowStock ?? false, take ?? 50), ct)));

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetInventoryItemQuery, InventoryItemResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetInventoryItemQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapPost("/{id:guid}/receipts", (Guid id, StockQuantityRequest r, IInventoryItemRepository repo, IUnitOfWork uow, CancellationToken ct) =>
            Mutate(id, repo, uow, i => i.Receive(r.Quantity, r.Reference), ct));

        group.MapPost("/{id:guid}/reservations", (Guid id, StockQuantityRequest r, IInventoryItemRepository repo, IUnitOfWork uow, CancellationToken ct) =>
            Mutate(id, repo, uow, i => i.Reserve(r.Quantity, r.Reference), ct));

        group.MapPost("/{id:guid}/reservations/release", (Guid id, StockQuantityRequest r, IInventoryItemRepository repo, IUnitOfWork uow, CancellationToken ct) =>
            Mutate(id, repo, uow, i => i.ReleaseReservation(r.Quantity, r.Reference), ct));

        group.MapPost("/{id:guid}/shipments", (Guid id, StockQuantityRequest r, IInventoryItemRepository repo, IUnitOfWork uow, CancellationToken ct) =>
            Mutate(id, repo, uow, i => i.Ship(r.Quantity, r.Reference), ct));

        group.MapPost("/{id:guid}/adjustments", (Guid id, StockAdjustmentRequest r, IInventoryItemRepository repo, IUnitOfWork uow, CancellationToken ct) =>
            Mutate(id, repo, uow, i => i.Adjust(r.CountedQuantity, r.Reason), ct));

        group.MapPut("/{id:guid}/reorder-point", (Guid id, ReorderPointRequest r, IInventoryItemRepository repo, IUnitOfWork uow, CancellationToken ct) =>
            Mutate(id, repo, uow, i => i.ChangeReorderPoint(r.ReorderPoint), ct));

        group.MapPost("/{id:guid}/discontinue", (Guid id, IInventoryItemRepository repo, IUnitOfWork uow, CancellationToken ct) =>
            Mutate(id, repo, uow, i => i.Discontinue(), ct));
    }

    /// <summary>Load -> apply one domain operation -> save. A concurrent change to the same item
    /// (detected via the row version) returns 409 so the client can reload and retry instead of overselling.</summary>
    private static async Task<IResult> Mutate(
        Guid id, IInventoryItemRepository repository, IUnitOfWork unitOfWork, Func<InventoryItem, Result> operation, CancellationToken ct)
    {
        var item = await repository.GetByIdAsync(id, ct);
        if (item is null) return Results.NotFound();

        var result = operation(item);
        if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new
            {
                Code = "Inventory.ConcurrentUpdate",
                Description = "The item was changed by another request; reload and try again."
            });
        }
        return Results.NoContent();
    }
}

public sealed record StockQuantityRequest(decimal Quantity, string Reference);
public sealed record StockAdjustmentRequest(decimal CountedQuantity, string Reason);
public sealed record ReorderPointRequest(decimal ReorderPoint);
