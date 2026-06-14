using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Orders.Application.Orders.CreateOrder;
using FleetIQ.Orders.Application.Orders.GetOrder;
using FluentValidation;

namespace FleetIQ.Orders.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders").RequireAuthorization();

        group.MapPost("/", async (
            CreateOrderCommand command,
            IValidator<CreateOrderCommand> validator,
            ICommandHandler<CreateOrderCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/orders/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetOrderQuery, OrderResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetOrderQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });
    }
}
