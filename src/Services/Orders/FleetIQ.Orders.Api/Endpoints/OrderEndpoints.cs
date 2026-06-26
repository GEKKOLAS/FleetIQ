using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Orders.Application.AddOrderItem;
using FleetIQ.Orders.Application.CancelOrder;
using FleetIQ.Orders.Application.ConfirmOrder;
using FleetIQ.Orders.Application.CreateOrder;
using FleetIQ.Orders.Application.GetOrder;
using FleetIQ.Orders.Application.ListOrders;
using FleetIQ.Orders.Application.MarkReadyForPlanning;
using FluentValidation;

namespace FleetIQ.Orders.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders").RequireAuthorization();

        group.MapPost("/", async (
            CreateOrderCommand command, IValidator<CreateOrderCommand> validator,
            ICommandHandler<CreateOrderCommand, Result<Guid>> handler, CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var result = await handler.Handle(command, ct);
            return result.IsSuccess ? Results.Created($"/orders/{result.Value}", new { id = result.Value }) : ToProblem(result.Error);
        });

        group.MapGet("/{id:guid}", async (
            Guid id, IQueryHandler<GetOrderQuery, OrderDetailResponse?> handler, CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetOrderQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapGet("/", async (
            Guid? customerId, string? status, int? page, int? pageSize, string? sort,
            IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>> handler, CancellationToken ct) =>
        {
            var query = new ListOrdersQuery(customerId, status, page ?? 1, pageSize ?? 20, sort);
            return Results.Ok(await handler.Handle(query, ct));
        });

        group.MapPost("/{id:guid}/items", async (
            Guid id, AddItemRequest body, IValidator<AddOrderItemCommand> validator,
            ICommandHandler<AddOrderItemCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new AddOrderItemCommand(id, body.Description, body.Quantity, body.WeightKg);
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var result = await handler.Handle(command, ct);
            return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);
        });

        group.MapPost("/{id:guid}/confirm", async (
            Guid id, ICommandHandler<ConfirmOrderCommand, Result> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new ConfirmOrderCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);
        });

        group.MapPost("/{id:guid}/ready-for-planning", async (
            Guid id, ICommandHandler<MarkReadyForPlanningCommand, Result> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new MarkReadyForPlanningCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);
        });

        group.MapPost("/{id:guid}/cancel", async (
            Guid id, ICommandHandler<CancelOrderCommand, Result> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new CancelOrderCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);
        });
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Code.EndsWith("NotFound")
            ? StatusCodes.Status404NotFound
            : error.Code is "Order.NotDraft" or "Order.NotConfirmed" or "Order.AlreadyCancelled"
                or "Order.CannotCancelInPlanning" or "Order.NoItems"
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
        return Results.Problem(detail: error.Description, title: error.Code, statusCode: status);
    }

    public sealed record AddItemRequest(string Description, int Quantity, decimal WeightKg);
}
