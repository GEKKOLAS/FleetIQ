using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Carriers.Application.AddDriver;
using FleetIQ.Carriers.Application.AddVehicle;
using FleetIQ.Carriers.Application.CreateCarrier;
using FleetIQ.Carriers.Application.GetCarrier;
using FleetIQ.Carriers.Application.ListCarriers;
using FleetIQ.Carriers.Application.ReactivateCarrier;
using FleetIQ.Carriers.Application.SuspendCarrier;
using FleetIQ.Carriers.Application.VerifyCarrier;
using FluentValidation;

namespace FleetIQ.Carriers.Api.Endpoints;

public static class CarrierEndpoints
{
    public static void MapCarrierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/carriers").WithTags("Carriers").RequireAuthorization();

        group.MapPost("/", async (CreateCarrierCommand command, IValidator<CreateCarrierCommand> validator,
            ICommandHandler<CreateCarrierCommand, Result<Guid>> handler, CancellationToken ct) =>
        {
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.Created($"/carriers/{r.Value}", new { id = r.Value }) : ToProblem(r.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetCarrierQuery, CarrierDetailResponse?> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new GetCarrierQuery(id), ct);
            return r is null ? Results.NotFound() : Results.Ok(r);
        });

        group.MapGet("/", async (string? status, int? page, int? pageSize,
            IQueryHandler<ListCarriersQuery, PagedResult<CarrierSummaryResponse>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ListCarriersQuery(status, page ?? 1, pageSize ?? 50), ct)));

        group.MapPost("/{id:guid}/vehicles", async (Guid id, VehicleBody body, IValidator<AddVehicleCommand> validator,
            ICommandHandler<AddVehicleCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new AddVehicleCommand(id, body.Plate, body.Type, body.CapacityKg);
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/drivers", async (Guid id, DriverBody body, IValidator<AddDriverCommand> validator,
            ICommandHandler<AddDriverCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new AddDriverCommand(id, body.FullName, body.LicenceNumber);
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/verify", async (Guid id, ICommandHandler<VerifyCarrierCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new VerifyCarrierCommand(id), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/suspend", async (Guid id, SuspendBody body, ICommandHandler<SuspendCarrierCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new SuspendCarrierCommand(id, body?.Reason ?? "n/a"), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/reactivate", async (Guid id, ICommandHandler<ReactivateCarrierCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new ReactivateCarrierCommand(id), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Code.EndsWith("NotFound") ? StatusCodes.Status404NotFound
            : error.Code is "Carrier.NotOnboarding" or "Carrier.NeedsFleet" or "Carrier.NotVerified"
              or "Carrier.NotSuspended" or "Carrier.Suspended"
                ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
        return Results.Problem(detail: error.Description, title: error.Code, statusCode: status);
    }

    public sealed record VehicleBody(string Plate, string Type, decimal CapacityKg);
    public sealed record DriverBody(string FullName, string LicenceNumber);
    public sealed record SuspendBody(string? Reason);
}
