using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Planning.Application.AddRouteStop;
using FleetIQ.Planning.Application.CreateRoute;
using FleetIQ.Planning.Application.FinalizeRoute;
using FleetIQ.Planning.Application.GetRoute;
using FluentValidation;

namespace FleetIQ.Planning.Api.Endpoints;

public static class RouteEndpoints
{
    public static void MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/routes").WithTags("Routes").RequireAuthorization();

        group.MapPost("/", async (CreateRouteCommand command, IValidator<CreateRouteCommand> validator,
            ICommandHandler<CreateRouteCommand, Result<Guid>> handler, CancellationToken ct) =>
        {
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.Created($"/routes/{r.Value}", new { id = r.Value }) : ToProblem(r.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetRouteQuery, RouteDetailResponse?> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new GetRouteQuery(id), ct);
            return r is null ? Results.NotFound() : Results.Ok(r);
        });

        group.MapPost("/{id:guid}/stops", async (Guid id, AddStopBody body, IValidator<AddRouteStopCommand> validator,
            ICommandHandler<AddRouteStopCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new AddRouteStopCommand(id, body.Kind, body.City, body.Address, body.Latitude, body.Longitude);
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/finalize", async (Guid id, ICommandHandler<FinalizeRouteCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new FinalizeRouteCommand(id), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Code.EndsWith("NotFound") ? StatusCodes.Status404NotFound
            : error.Code is "Route.NotDraft" or "Route.TooFewStops" ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;
        return Results.Problem(detail: error.Description, title: error.Code, statusCode: status);
    }

    public sealed record AddStopBody(string Kind, string City, string Address, double Latitude, double Longitude);
}
