using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Planning.Application.AddLoadOrder;
using FleetIQ.Planning.Application.AssignCarrier;
using FleetIQ.Planning.Application.AttachRoute;
using FleetIQ.Planning.Application.CancelLoad;
using FleetIQ.Planning.Application.ConfirmLoad;
using FleetIQ.Planning.Application.CreateLoad;
using FleetIQ.Planning.Application.GetLoad;
using FleetIQ.Planning.Application.ListLoads;
using FleetIQ.Planning.Application.PlanLoad;
using FluentValidation;

namespace FleetIQ.Planning.Api.Endpoints;

public static class LoadEndpoints
{
    public static void MapLoadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/loads").WithTags("Loads").RequireAuthorization();

        group.MapPost("/", async (CreateLoadCommand command, IValidator<CreateLoadCommand> validator,
            ICommandHandler<CreateLoadCommand, Result<Guid>> handler, CancellationToken ct) =>
        {
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.Created($"/loads/{r.Value}", new { id = r.Value }) : ToProblem(r.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetLoadQuery, LoadDetailResponse?> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new GetLoadQuery(id), ct);
            return r is null ? Results.NotFound() : Results.Ok(r);
        });

        group.MapGet("/", async (string? status, int? page, int? pageSize,
            IQueryHandler<ListLoadsQuery, PagedResult<LoadSummaryResponse>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ListLoadsQuery(status, page ?? 1, pageSize ?? 50), ct)));

        group.MapPost("/{id:guid}/orders", async (Guid id, AddOrderBody body, IValidator<AddLoadOrderCommand> validator,
            ICommandHandler<AddLoadOrderCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new AddLoadOrderCommand(id, body.OrderId, body.Description, body.WeightKg);
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/assign-carrier", async (Guid id, AssignCarrierBody body, IValidator<AssignCarrierCommand> validator,
            ICommandHandler<AssignCarrierCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new AssignCarrierCommand(id, body.CarrierId);
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/attach-route", async (Guid id, AttachRouteBody body, IValidator<AttachRouteCommand> validator,
            ICommandHandler<AttachRouteCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new AttachRouteCommand(id, body.RouteId);
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/plan", async (Guid id, ICommandHandler<PlanLoadCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new PlanLoadCommand(id), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/confirm", async (Guid id, ICommandHandler<ConfirmLoadCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new ConfirmLoadCommand(id), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, ICommandHandler<CancelLoadCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new CancelLoadCommand(id), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Code.EndsWith("NotFound") ? StatusCodes.Status404NotFound
            : error.Code is "Load.NotDraft" or "Load.NotPlanned" or "Load.AlreadyFinished"
              or "Load.NoOrders" or "Load.NoCarrier" or "Load.NoRoute"
              or "Route.NotFinalized" or "Route.WrongLoad" or "Load.CarrierNotAssignable"
                ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
        return Results.Problem(detail: error.Description, title: error.Code, statusCode: status);
    }

    public sealed record AddOrderBody(Guid OrderId, string Description, decimal WeightKg);
    public sealed record AssignCarrierBody(Guid CarrierId);
    public sealed record AttachRouteBody(Guid RouteId);
}
